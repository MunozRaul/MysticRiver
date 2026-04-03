using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using AutoUpdaterDotNET;

namespace MysticRiver.Client;

public class UpdateService {
    private readonly string _repoOwner = "munozraul";
    private readonly string _repoName = "mysticriver";

    public void CheckForUpdates() {
        if (Debugger.IsAttached) {
            // checking without #ifdef because otherwise analyzers and linters would
            // mark code below as unreachable and having trouble formatting
            return;
        }

        try {
            var latestReleaseApiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";

            var latest = FetchLatestRelease(latestReleaseApiUrl);
            if (latest is null) {
                return;
            }

            var installed = GetInstalledVersion();
            if (!Version.TryParse(latest.Version, out var latestVersion)) {
                Debug.WriteLine($"Latest release version is invalid: {latest.Version}");
                return;
            }

            if (latestVersion <= installed) {
                return;
            }

            AutoUpdater.HttpUserAgent = "MysticRiver";
            AutoUpdater.ReportErrors = true;
            AutoUpdater.ShowUpdateForm(new UpdateInfoEventArgs {
                CurrentVersion = latest.Version,
                DownloadURL = latest.DownloadUrl,
                ChangelogURL = latest.ChangelogUrl
            });
        }
        catch (Exception ex) {
            Debug.WriteLine($"Update check failed: {ex}");
        }
    }

    private static ReleaseInfo? FetchLatestRelease(string latestReleaseApiUrl) {
        try {
            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MysticRiver");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var remoteData = client.GetStringAsync(latestReleaseApiUrl).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(remoteData);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElement)) {
                var message = root.TryGetProperty("message", out var msgElement)
                    ? msgElement.GetString() ?? "missing tag_name"
                    : "missing tag_name";
                Debug.WriteLine($"Update metadata does not contain tag_name: {message}");
                return null;
            }

            var tagName = tagElement.GetString() ?? string.Empty;
            var version = NormalizeVersion(tagName);

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array) {
                foreach (var asset in assets.EnumerateArray()) {
                    var name = asset.TryGetProperty("name", out var nameElement)
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;

                    if (name.Contains("client", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                        asset.TryGetProperty("browser_download_url", out var downloadElement)) {
                        downloadUrl = downloadElement.GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(downloadUrl)) {
                Debug.WriteLine("Update metadata is missing a valid version or client zip asset.");
                return null;
            }

            var changelogUrl = root.TryGetProperty("html_url", out var htmlUrl)
                ? htmlUrl.GetString()
                : null;

            return new ReleaseInfo(version, downloadUrl, changelogUrl);
        }
        catch (JsonException ex) {
            Debug.WriteLine($"Update metadata JSON parse failed: {ex.Message}");
            return null;
        }
        catch (Exception ex) {
            Debug.WriteLine($"Update metadata parse failed: {ex}");
            return null;
        }
    }

    private static Version GetInstalledVersion() {
        var version = Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version;

        return version ?? new Version(0, 0, 0, 0);
    }

    private static string? NormalizeVersion(string tagName) {
        var cleaned = tagName.Trim().TrimStart('v', 'V');

        var match = Regex.Match(cleaned, @"^(\d+\.\d+\.\d+)(?:\.(\d+))?$");
        if (!match.Success) {
            return null;
        }

        cleaned = match.Groups[2].Success ? match.Value : $"{match.Groups[1].Value}.0";

        if (Version.TryParse(cleaned, out var parsed)) {
            return parsed.ToString();
        }

        return null;
    }

    private sealed record ReleaseInfo(string Version, string DownloadUrl, string? ChangelogUrl);
}
