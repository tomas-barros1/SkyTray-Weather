namespace WinuiWheaterForecastTray.Constants;

/// <summary>
/// Centralized HTTP API endpoint URLs used across Core services.
/// </summary>
internal static class EndpointUrls
{
    public const string OpenMeteoForecast = "https://api.open-meteo.com/v1/forecast";
    public const string OpenMeteoAirQuality = "https://air-quality-api.open-meteo.com/v1/air-quality";
    public const string BigDataCloudReverseGeocode = "https://api.bigdatacloud.net/data/reverse-geocode-client";
    public const string IpApiCoJson = "https://ipapi.co/json/";
    public const string IpApiComJson = "http://ip-api.com/json/";
}
