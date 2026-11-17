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

        public async Task<GitHubUpdateChecker.AvailableUpdate?> CheckForUpdateAsync() =>
            await new GitHubUpdateChecker(HttpClient).CheckForUpdateAsync(GetCurrentVersion() ?? new Version(1, 0));

        public async Task InstallUpdateAsync(GitHubUpdateChecker.AvailableUpdate update)
        {
            string installerPath = Path.Combine(Path.GetTempPath(), $"SkyTrayWeather-Setup-{update.Version}.exe");
            using HttpResponseMessage response = await HttpClient.GetAsync(update.InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using (Stream source = await response.Content.ReadAsStreamAsync())
            await using (FileStream destination = File.Create(installerPath))
                await source.CopyToAsync(destination);

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART",
                UseShellExecute = true
            });
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
