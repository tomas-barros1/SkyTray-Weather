using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Core.Services;

namespace WinuiWeatherForecastTray.Services;

/// <summary>Checks the latest stable GitHub release for a newer Windows installer.</summary>
public sealed class GitHubUpdateChecker
{
    public const string LatestReleaseUrl = AppUpdateConstants.LatestReleaseUrl;
    public const string InstallerAssetName = AppUpdateConstants.InstallerAssetName;

    private readonly HttpClient _httpClient;

    public GitHubUpdateChecker(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<AvailableUpdate?> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        if (currentVersion.Major == 0)
            return null;

        using HttpResponseMessage response = await _httpClient.GetAsync(LatestReleaseUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (release?.TagName is null || release.Prerelease || !TryParseStableVersion(release.TagName, out Version? latestVersion))
            return null;

        string? installerUrl = release.Assets?.Find(asset => asset.Name == InstallerAssetName)?.DownloadUrl;
        string? checksumsUrl = release.Assets?.Find(asset => asset.Name == AppUpdateConstants.ChecksumsAssetName)?.DownloadUrl;
        string releasePageUrl = $"https://github.com/{AppUpdateConstants.RepositoryOwner}/{AppUpdateConstants.RepositoryName}/releases/tag/{Uri.EscapeDataString(release.TagName)}";
        // SDK-generated assembly versions commonly have a trailing .0 (1.6.2.0),
        // while GitHub tags omit it (1.6.2). System.Version treats those as
        // different, with 1.6.2.0 greater than 1.6.2, so compare padded versions.
        if (NormalizeVersion(latestVersion) <= NormalizeVersion(currentVersion) ||
            !UpdatePackageVerifier.IsTrustedInstallerUrl(installerUrl) ||
            (checksumsUrl is not null && (!UpdatePackageVerifier.IsTrustedChecksumsUrl(checksumsUrl) ||
                !UpdatePackageVerifier.AreAssetsFromSameRelease(installerUrl!, checksumsUrl))) ||
            !UpdatePackageVerifier.IsAssetForTag(installerUrl!, release.TagName) ||
            (checksumsUrl is not null && !UpdatePackageVerifier.IsAssetForTag(checksumsUrl, release.TagName)))
            return null;

        return new AvailableUpdate(latestVersion, installerUrl!, checksumsUrl, releasePageUrl);
    }

    private static bool TryParseStableVersion(string tag, out Version version)
    {
        version = new Version(0, 0);
        string normalized = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;
        int metadataIndex = normalized.IndexOf('+');
        if (metadataIndex >= 0)
            normalized = normalized[..metadataIndex];

        if (normalized.Contains('-'))
            return false;

        if (Version.TryParse(normalized, out Version? parsed) && parsed is not null)
        {
            version = parsed;
            return true;
        }

        return false;
    }

    private static Version NormalizeVersion(Version version) => new(
        version.Major,
        version.Minor,
        Math.Max(version.Build, 0),
        Math.Max(version.Revision, 0));

    public sealed record AvailableUpdate(Version Version, string InstallerUrl, string? ChecksumsUrl, string ReleasePageUrl);

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

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
