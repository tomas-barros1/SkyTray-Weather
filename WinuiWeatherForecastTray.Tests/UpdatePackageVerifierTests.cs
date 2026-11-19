using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WinuiWeatherForecastTray.Core.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public sealed class UpdatePackageVerifierTests
{
    private const string InstallerUrl = "https://github.com/tomas-barros1/SkyTray-Weather/releases/download/v2.0.0/SkyTrayWeather-Setup-x64.exe";
    private const string ChecksumsUrl = "https://github.com/tomas-barros1/SkyTray-Weather/releases/download/v2.0.0/SHA256SUMS.txt";

    [Fact]
    public void UpdateLaunchCommandBuilder_CreatesSilentDetachedInstallAndRestartCommand()
    {
        string arguments = UpdateLaunchCommandBuilder.Create(
            @"C:\Users\Tom\AppData\Local\Temp\SkyTray Setup.exe",
            @"C:\Users\Tom\AppData\Local\SkyTrayWeather\WinuiWeatherForecastTray.exe");

        arguments.Should().Be("/c \"\"C:\\Users\\Tom\\AppData\\Local\\Temp\\SkyTray Setup.exe\" /SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS && start \"\" \"C:\\Users\\Tom\\AppData\\Local\\SkyTrayWeather\\WinuiWeatherForecastTray.exe\"\"");
    }

    [Theory]
    [InlineData("C:\\Unsafe%Path\\installer.exe", "C:\\app.exe")]
    [InlineData("C:\\installer.exe", "C:\\Unsafe%Path\\app.exe")]
    [InlineData("C:\\Unsafe\"Path\\installer.exe", "C:\\app.exe")]
    [InlineData("C:\\installer.exe", "C:\\Unsafe\"Path\\app.exe")]
    public void UpdateLaunchCommandBuilder_RejectsQuoteAndPercentInPaths(string installerPath, string applicationPath)
    {
        Action action = () => UpdateLaunchCommandBuilder.Create(installerPath, applicationPath);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsTrustedInstallerUrl_RejectsAnotherRepository()
    {
        UpdatePackageVerifier.IsTrustedInstallerUrl("https://github.com/attacker/Other/releases/download/v2.0.0/SkyTrayWeather-Setup-x64.exe")
            .Should().BeFalse();
    }

    [Fact]
    public void TryGetExpectedSha256_AcceptsUppercaseHashCrLfAndBinaryMarker()
    {
        string expected = new string('A', 64);

        bool found = UpdatePackageVerifier.TryGetExpectedSha256(
            $"{expected} *{AppUpdateConstants.InstallerAssetName}\r\n", AppUpdateConstants.InstallerAssetName, out byte[] hash);

        found.Should().BeTrue();
        hash.Should().Equal(Convert.FromHexString(expected));
    }

    [Fact]
    public void TryGetExpectedSha256_AcceptsSha256sumWhitespaceMarker()
    {
        string expected = new string('a', 64);

        bool found = UpdatePackageVerifier.TryGetExpectedSha256(
            $"{expected}  {AppUpdateConstants.InstallerAssetName}\n", AppUpdateConstants.InstallerAssetName, out byte[] hash);

        found.Should().BeTrue();
        hash.Should().Equal(Convert.FromHexString(expected));
    }

    [Fact]
    public async Task DownloadVerifiedInstallerAsync_WhenHashIsWrong_DeletesPartialAndDestinationFiles()
    {
        byte[] installerBytes = Encoding.UTF8.GetBytes("installer bytes");
        string wrongHash = new string('0', 64);
        using HttpClient client = CreateClient($"{wrongHash}  {AppUpdateConstants.InstallerAssetName}\n", installerBytes);
        string directory = CreateTempDirectory();
        string destination = Path.Combine(directory, AppUpdateConstants.InstallerAssetName);
        await File.WriteAllTextAsync(destination, "old file");

        Func<Task> action = () => UpdatePackageVerifier.DownloadVerifiedInstallerAsync(client, InstallerUrl, ChecksumsUrl, destination);

        await action.Should().ThrowAsync<InvalidDataException>();
        File.Exists(destination).Should().BeFalse();
        File.Exists(destination + ".download").Should().BeFalse();
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task DownloadVerifiedInstallerAsync_WhenChecksumEntryIsMissing_LeavesNoFile()
    {
        byte[] installerBytes = Encoding.UTF8.GetBytes("installer bytes");
        using HttpClient client = CreateClient(new string('a', 64) + "  another-file.exe\n", installerBytes);
        string directory = CreateTempDirectory();
        string destination = Path.Combine(directory, AppUpdateConstants.InstallerAssetName);

        Func<Task> action = () => UpdatePackageVerifier.DownloadVerifiedInstallerAsync(client, InstallerUrl, ChecksumsUrl, destination);

        await action.Should().ThrowAsync<InvalidDataException>();
        Directory.GetFiles(directory).Should().BeEmpty();
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task DownloadVerifiedInstallerAsync_WhenHashMatches_RenamesVerifiedFile()
    {
        byte[] installerBytes = Encoding.UTF8.GetBytes("installer bytes");
        string hash = Convert.ToHexString(SHA256.HashData(installerBytes)).ToLowerInvariant();
        using HttpClient client = CreateClient($"{hash}  {AppUpdateConstants.InstallerAssetName}\n", installerBytes);
        string directory = CreateTempDirectory();
        string destination = Path.Combine(directory, AppUpdateConstants.InstallerAssetName);

        await UpdatePackageVerifier.DownloadVerifiedInstallerAsync(client, InstallerUrl, ChecksumsUrl, destination);

        (await File.ReadAllBytesAsync(destination)).Should().Equal(installerBytes);
        File.Exists(destination + ".download").Should().BeFalse();
        Directory.Delete(directory, recursive: true);
    }

    private static HttpClient CreateClient(string checksums, byte[] installerBytes) => new(new MockHttpMessageHandler(request =>
    {
        if (request.RequestUri!.AbsoluteUri == ChecksumsUrl)
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(checksums, Encoding.UTF8) };
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(installerBytes) };
    }));

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "SkyTrayUpdateTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
