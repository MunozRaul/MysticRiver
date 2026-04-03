using AutoUpdaterDotNET;
using System.Diagnostics;
using System.Text.Json;

namespace MysticRiver.Client
{
    public class UpdateService
    {
        private readonly string _repoOwner = "munozraul";
        private readonly string _repoName = "mysticriver";

        public void CheckForUpdates()
        {
            try
            {
                string latestReleaseApiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";

                AutoUpdater.HttpUserAgent = "MysticRiver";
                AutoUpdater.ReportErrors = true;

                AutoUpdater.ParseUpdateInfoEvent += ParseGitHubRelease;
                try
                {
                    AutoUpdater.Start(latestReleaseApiUrl);
                }
                finally
                {
                    AutoUpdater.ParseUpdateInfoEvent -= ParseGitHubRelease;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private void ParseGitHubRelease(ParseUpdateInfoEventArgs args)
        {
            using JsonDocument doc = JsonDocument.Parse(args.RemoteData);
            JsonElement root = doc.RootElement;

            string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string version = tagName;

            string? downloadUrl = null;
            foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Contains("client", StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(downloadUrl))
            {
                Debug.WriteLine("Update metadata is missing version or client zip asset.");
                return;
            }

            string? changelogUrl = root.TryGetProperty("html_url", out JsonElement htmlUrl)
                ? htmlUrl.GetString()
                : null;

            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = version,
                DownloadURL = downloadUrl,
                ChangelogURL = changelogUrl
            };
        }
    }
}
