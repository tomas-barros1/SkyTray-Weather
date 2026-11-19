using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using WinuiWeatherForecastTray.Core.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public sealed class GitHubUpdateCheckerTests
{
    private const string InstallerUrl = "https://github.com/tomas-barros1/SkyTray-Weather/releases/download/v1.2.0/SkyTrayWeather-Setup-x64.exe";

    [Fact]
    public async Task CheckForUpdateAsync_WhenLatestReleaseIsNewer_ReturnsInstaller()
    {
        var checker = CreateChecker(Release("v1.2.0", InstallerUrl));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 1, 0));

        update.Should().NotBeNull();
        update!.Version.Should().Be(new Version(1, 2, 0));
        update.InstallerUrl.Should().Be(InstallerUrl);
    }

    [Theory]
    [InlineData("v1.6.10", 1, 6, 9)]
    [InlineData("v1.6.10+sha.abc", 1, 6, 9)]
    public async Task CheckForUpdateAsync_WhenStableSemanticVersionIsNewer_ReturnsUpdate(string tag, int major, int minor, int build)
    {
        var checker = CreateChecker(ReleaseForTag(tag));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(major, minor, build));

        update.Should().NotBeNull();
        update!.Version.Should().Be(new Version(1, 6, 10));
    }

    [Theory]
    [InlineData("v1.7.0-beta.1", false)]
    [InlineData("v1.7.0", true)]
    public async Task CheckForUpdateAsync_WhenReleaseIsPrerelease_ReturnsNull(string tag, bool prerelease)
    {
        var checker = CreateChecker(ReleaseForTag(tag, prerelease));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 6, 9));

        update.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenCurrentVersionIsDevelopment_DoesNotCallNetwork()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Network should not be called."));
        var checker = new GitHubUpdateChecker(new HttpClient(handler));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(0, 0, 0, 0));

        update.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenInstalledVersionHasTrailingZeroRevision_DoesNotOfferSameRelease()
    {
        const string currentReleaseInstallerUrl = "https://github.com/tomas-barros1/SkyTray-Weather/releases/download/v1.6.2/SkyTrayWeather-Setup-x64.exe";
        var checker = CreateChecker(Release("v1.6.2", currentReleaseInstallerUrl));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 6, 2, 0));

        update.Should().BeNull();
    }

    [Theory]
    [InlineData("v1.1.0")]
    [InlineData("v1.0.0")]
    public async Task CheckForUpdateAsync_WhenReleaseIsNotNewer_ReturnsNull(string tag)
    {
        var checker = CreateChecker(Release(tag, InstallerUrl));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 1, 0));

        update.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenInstallerAssetIsMissing_ReturnsNull()
    {
        var checker = CreateChecker("""{"tag_name":"v1.2.0","assets":[]}""");

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 1, 0));

        update.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenInstallerUrlIsNotTrustedHttpsGitHub_ReturnsNull()
    {
        var checker = CreateChecker(Release("v1.2.0", "http://example.com/SkyTrayWeather-Setup-x64.exe"));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 1, 0));

        update.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenReleaseTagIsNotAValidVersion_ReturnsNull()
    {
        var checker = CreateChecker(Release("latest", InstallerUrl));

        GitHubUpdateChecker.AvailableUpdate? update = await checker.CheckForUpdateAsync(new Version(1, 1, 0));

        update.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdateAsync_RequestsLatestReleaseEndpoint()
    {
        Uri? requestedUri = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Release("v1.2.0", InstallerUrl))
            };
        });
        var checker = new GitHubUpdateChecker(new HttpClient(handler));

        await checker.CheckForUpdateAsync(new Version(1, 1, 0));

        requestedUri.Should().Be(new Uri(GitHubUpdateChecker.LatestReleaseUrl));
    }

    private static GitHubUpdateChecker CreateChecker(string json) =>
        new(new HttpClient(new MockHttpMessageHandler(json)));

    private static string Release(string tag, string installerUrl) =>
        $$"""{"tag_name":"{{tag}}","assets":[{"name":"SkyTrayWeather-Setup-x64.exe","browser_download_url":"{{installerUrl}}"}]}""";

    private static string ReleaseForTag(string tag, bool prerelease = false)
    {
        string encodedTag = Uri.EscapeDataString(tag);
        string installer = $"https://github.com/tomas-barros1/SkyTray-Weather/releases/download/{encodedTag}/{GitHubUpdateChecker.InstallerAssetName}";
        return $$"""{"tag_name":"{{tag}}","prerelease":{{prerelease.ToString().ToLowerInvariant()}},"assets":[{"name":"{{GitHubUpdateChecker.InstallerAssetName}}","browser_download_url":"{{installer}}"}]}""";
    }
}
