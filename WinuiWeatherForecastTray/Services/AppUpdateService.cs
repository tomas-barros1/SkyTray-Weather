using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Core.Services;

namespace WinuiWeatherForecastTray.Services
{
    internal sealed class AppUpdateService
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        public bool IsDevelopmentBuild => GetCurrentVersion()?.Major == 0;

        public async Task<GitHubUpdateChecker.AvailableUpdate?> CheckForUpdateAsync() =>
            await new GitHubUpdateChecker(HttpClient).CheckForUpdateAsync(GetCurrentVersion() ?? new Version(1, 0));

        public async Task<bool> InstallUpdateAsync(GitHubUpdateChecker.AvailableUpdate update, IProgress<double>? progress = null)
        {
            if (update.ChecksumsUrl is null)
            {
                Process.Start(new ProcessStartInfo(update.ReleasePageUrl) { UseShellExecute = true });
                return false;
            }

            string installerPath = Path.Combine(Path.GetTempPath(), $"SkyTrayWeather-Setup-{update.Version}.exe");
            await UpdatePackageVerifier.DownloadVerifiedInstallerAsync(
                HttpClient, update.InstallerUrl, update.ChecksumsUrl, installerPath, progress: progress);

            string? applicationPath = Environment.ProcessPath;
            if (applicationPath is null)
                throw new InvalidOperationException("Could not determine the application executable path.");

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = UpdateLaunchCommandBuilder.Create(installerPath, applicationPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            return true;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SkyTrayWeather", GetCurrentVersion()?.ToString() ?? "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        private static Version? GetCurrentVersion() => Assembly.GetEntryAssembly()?.GetName().Version;

    }
}
