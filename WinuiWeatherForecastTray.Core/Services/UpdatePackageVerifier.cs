using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Services;

namespace WinuiWeatherForecastTray.Core.Services;

public static class UpdatePackageVerifier
{
    public static bool IsTrustedReleaseAssetUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(uri.UserInfo) && uri.IsDefaultPort &&
            string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
            uri.AbsolutePath.StartsWith(AppUpdateConstants.ReleaseAssetPathPrefix, StringComparison.Ordinal);
    }

    public static bool IsTrustedInstallerUrl(string? value) =>
        IsTrustedReleaseAssetUrl(value) && new Uri(value!).AbsolutePath.EndsWith("/" + AppUpdateConstants.InstallerAssetName, StringComparison.Ordinal);

    public static bool IsTrustedChecksumsUrl(string? value) =>
        IsTrustedReleaseAssetUrl(value) && new Uri(value!).AbsolutePath.EndsWith("/" + AppUpdateConstants.ChecksumsAssetName, StringComparison.Ordinal);

    public static bool AreAssetsFromSameRelease(string first, string second) => SameReleaseDirectory(first, second);

    public static bool IsAssetForTag(string url, string tag)
    {
        if (!IsTrustedReleaseAssetUrl(url))
            return false;
        string expectedPrefix = AppUpdateConstants.ReleaseAssetPathPrefix + Uri.EscapeDataString(tag) + "/";
        return new Uri(url).AbsolutePath.StartsWith(expectedPrefix, StringComparison.Ordinal);
    }

    public static bool TryGetExpectedSha256(string? checksums, string fileName, out byte[] expectedHash)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        expectedHash = Array.Empty<byte>();
        if (checksums is null)
            return false;

        foreach (string rawLine in checksums.Split('\n'))
        {
            string line = rawLine.EndsWith('\r') ? rawLine[..^1] : rawLine;
            if (line.Length < 66 || line[64] != ' ')
                continue;

            int nameOffset;
            if (line.Length > 65 && line[65] == ' ')
                nameOffset = 66;
            else if (line.Length > 65 && line[65] == '*')
                nameOffset = 66;
            else
                continue;

            if (!string.Equals(line[nameOffset..], fileName, StringComparison.Ordinal))
                continue;

            try
            {
                byte[] hash = Convert.FromHexString(line[..64]);
                if (hash.Length != 32)
                    return false;
                expectedHash = hash;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return false;
    }

    public static async Task DownloadVerifiedInstallerAsync(
        HttpClient httpClient,
        string installerUrl,
        string? checksumsUrl,
        string destinationPath,
        CancellationToken cancellationToken = default,
        IProgress<double>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(destinationPath);
        string downloadPath = destinationPath + ".download";

        try
        {
            if (!IsTrustedInstallerUrl(installerUrl) || !IsTrustedChecksumsUrl(checksumsUrl) ||
                !SameReleaseDirectory(installerUrl, checksumsUrl!))
                throw new InvalidDataException("The release assets do not use trusted URLs.");

            byte[] expectedHash;
            using (HttpResponseMessage checksumResponse = await httpClient.GetAsync(checksumsUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                checksumResponse.EnsureSuccessStatusCode();
                if (checksumResponse.Content.Headers.ContentLength > AppUpdateConstants.MaximumChecksumsSizeBytes)
                    throw new InvalidDataException("The checksum file is too large.");

                byte[] checksumBytes = await ReadBoundedAsync(checksumResponse.Content, AppUpdateConstants.MaximumChecksumsSizeBytes, cancellationToken).ConfigureAwait(false);
                string checksumText = Encoding.UTF8.GetString(checksumBytes);
                if (!TryGetExpectedSha256(checksumText, AppUpdateConstants.InstallerAssetName, out expectedHash))
                    throw new InvalidDataException("The installer checksum is missing or invalid.");
            }

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using HttpResponseMessage installerResponse = await httpClient.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            installerResponse.EnsureSuccessStatusCode();
            if (installerResponse.Content.Headers.ContentLength > AppUpdateConstants.MaximumInstallerSizeBytes)
                throw new InvalidDataException("The installer is too large.");

            await using Stream source = await installerResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (var destination = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                byte[] buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > AppUpdateConstants.MaximumInstallerSizeBytes)
                        throw new InvalidDataException("The installer is too large.");

                    hash.AppendData(buffer, 0, read);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    if (installerResponse.Content.Headers.ContentLength is long length && length > 0)
                        progress?.Report(Math.Min(1d, (double)total / length));
                }
            }

            byte[] actualHash = hash.GetHashAndReset();
            if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
                throw new InvalidDataException("The installer checksum does not match.");

            File.Move(downloadPath, destinationPath, overwrite: true);
        }
        catch
        {
            TryDelete(downloadPath);
            TryDelete(destinationPath);
            throw;
        }
    }

    private static bool SameReleaseDirectory(string first, string second)
    {
        var firstUri = new Uri(first);
        var secondUri = new Uri(second);
        string firstDirectory = firstUri.AbsolutePath[..(firstUri.AbsolutePath.LastIndexOf('/') + 1)];
        string secondDirectory = secondUri.AbsolutePath[..(secondUri.AbsolutePath.LastIndexOf('/') + 1)];
        return string.Equals(firstDirectory, secondDirectory, StringComparison.Ordinal);
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken)
    {
        await using Stream source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = new MemoryStream();
        byte[] buffer = new byte[4096];
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (destination.Length + read > maximumBytes)
                throw new InvalidDataException("The checksum file is too large.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException ex)
        {
            DebugLog.Swallowed(typeof(UpdatePackageVerifier), ex, "Failed to delete an unverified update file");
        }
        catch (UnauthorizedAccessException ex)
        {
            DebugLog.Swallowed(typeof(UpdatePackageVerifier), ex, "Failed to delete an unverified update file");
        }
    }
}
