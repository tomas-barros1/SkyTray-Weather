namespace WinuiWeatherForecastTray.Core.Services;

public static class AppUpdateConstants
{
    public const string RepositoryOwner = "tomas-barros1";
    public const string RepositoryName = "SkyTray-Weather";
    public const string LatestReleaseUrl = "https://api.github.com/repos/tomas-barros1/SkyTray-Weather/releases/latest";
    public const string InstallerAssetName = "SkyTrayWeather-Setup-x64.exe";
    public const string ChecksumsAssetName = "SHA256SUMS.txt";
    public const string ReleaseAssetPathPrefix = "/tomas-barros1/SkyTray-Weather/releases/download/";
    public const long MaximumInstallerSizeBytes = 100L * 1024 * 1024;
    public const int MaximumChecksumsSizeBytes = 64 * 1024;
    public static readonly TimeSpan InitialUpdateCheckDelay = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan AutomaticUpdateCheckInterval = TimeSpan.FromHours(6);
    public static readonly TimeSpan MinimumUpdateCheckInterval = TimeSpan.FromHours(20);
}
