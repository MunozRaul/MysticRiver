using AutoUpdaterDotNET;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MysticRiver.Client
{
    public class UpdateService
    {
        private readonly string _repoOwner = "munozraul";
        private readonly string _repoName = "mysticriver";

        public void CheckForUpdates()
        {
#if !DEBUG
            try
            {
                string latestReleaseApiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";

                ReleaseInfo? latest = FetchLatestRelease(latestReleaseApiUrl);
                if (latest is null)
                {
                    return;
                }

                Version installed = GetInstalledVersion();
                if (!Version.TryParse(latest.Version, out Version? latestVersion))
                {
                    Debug.WriteLine($"Latest release version is invalid: {latest.Version}");
                    return;
                }

                if (latestVersion <= installed)
                {
                    return;
                }

                AutoUpdater.HttpUserAgent = "MysticRiver";
                AutoUpdater.ReportErrors = true;
                AutoUpdater.ShowUpdateForm(new UpdateInfoEventArgs
                {
                    CurrentVersion = latest.Version,
                    DownloadURL = latest.DownloadUrl,
                    ChangelogURL = latest.ChangelogUrl
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex}");
            }
#endif
        }

        private static ReleaseInfo? FetchLatestRelease(string latestReleaseApiUrl)
        {
            try
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("MysticRiver");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                string remoteData = client.GetStringAsync(latestReleaseApiUrl).GetAwaiter().GetResult();
                using JsonDocument doc = JsonDocument.Parse(remoteData);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("tag_name", out JsonElement tagElement))
                {
                    string message = root.TryGetProperty("message", out JsonElement msgElement)
                        ? msgElement.GetString() ?? "missing tag_name"
                        : "missing tag_name";
                    Debug.WriteLine($"Update metadata does not contain tag_name: {message}");
                    return null;
                }

                string tagName = tagElement.GetString() ?? string.Empty;
                if (!TryNormalizeVersion(tagName, out string? version))
                {
                    Debug.WriteLine($"Release tag is not a valid semantic version: {tagName}");
                    return null;
                }

                string? downloadUrl = null;
                if (root.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string name = asset.TryGetProperty("name", out JsonElement nameElement)
                            ? nameElement.GetString() ?? string.Empty
                            : string.Empty;

                        if (name.Contains("client", StringComparison.OrdinalIgnoreCase) &&
                            name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                            asset.TryGetProperty("browser_download_url", out JsonElement downloadElement))
                        {
                            downloadUrl = downloadElement.GetString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    Debug.WriteLine("Update metadata is missing a valid version or client zip asset.");
                    return null;
                }

                string? changelogUrl = root.TryGetProperty("html_url", out JsonElement htmlUrl)
                    ? htmlUrl.GetString()
                    : null;

                return new ReleaseInfo(version, downloadUrl, changelogUrl);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Update metadata JSON parse failed: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update metadata parse failed: {ex}");
                return null;
            }
        }

        private static Version GetInstalledVersion()
        {
            Version? version = Assembly.GetEntryAssembly()?.GetName().Version
                ?? Assembly.GetExecutingAssembly().GetName().Version;

            return version ?? new Version(0, 0, 0, 0);
        }

        private static bool TryNormalizeVersion(string tagName, [NotNullWhen(true)] out string? normalized)
        {
            normalized = null;

            string cleaned = tagName.Trim().TrimStart('v', 'V');

            Match match = Regex.Match(cleaned, @"^(\d+\.\d+\.\d+)(?:\.(\d+))?$");
            if (!match.Success)
            {
                return false;
            }

            cleaned = match.Groups[2].Success ? match.Value : $"{match.Groups[1].Value}.0";

            if (Version.TryParse(cleaned, out Version? parsed))
            {
                normalized = parsed.ToString();
                return true;
            }

            return false;
        }

        private sealed record ReleaseInfo(string Version, string DownloadUrl, string? ChangelogUrl);
    }
}
