using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WinuiWeatherForecastTray.Core.Services;

/// <summary>Checks the latest stable GitHub release for a newer Windows installer.</summary>
public sealed class GitHubUpdateChecker
{
    public const string LatestReleaseUrl = "https://api.github.com/repos/tomas-barros1/SkyTray-Weather/releases/latest";
    public const string InstallerAssetName = "SkyTrayWeather-Setup-x64.exe";

    private readonly HttpClient _httpClient;

    public GitHubUpdateChecker(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<AvailableUpdate?> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        using HttpResponseMessage response = await _httpClient.GetAsync(LatestReleaseUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (release?.TagName is null || !Version.TryParse(NormalizeVersion(release.TagName), out Version? latestVersion))
            return null;

        string? installerUrl = release.Assets?.Find(asset => asset.Name == InstallerAssetName)?.DownloadUrl;
        // SDK-generated assembly versions commonly have a trailing .0 (1.6.2.0),
        // while GitHub tags omit it (1.6.2). System.Version treats those as
        // different, with 1.6.2.0 greater than 1.6.2, so compare padded versions.
        if (NormalizeVersion(latestVersion) <= NormalizeVersion(currentVersion) || !Uri.TryCreate(installerUrl, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com")
            return null;

        return new AvailableUpdate(latestVersion, uri.AbsoluteUri);
    }

    private static string NormalizeVersion(string tag) => tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;

    private static Version NormalizeVersion(Version version) => new(
        version.Major,
        version.Minor,
        Math.Max(version.Build, 0),
        Math.Max(version.Revision, 0));

    public sealed record AvailableUpdate(Version Version, string InstallerUrl);

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("assets")]
        public System.Collections.Generic.List<ReleaseAsset>? Assets { get; init; }
    }

    private sealed class ReleaseAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? DownloadUrl { get; init; }
    }
}
