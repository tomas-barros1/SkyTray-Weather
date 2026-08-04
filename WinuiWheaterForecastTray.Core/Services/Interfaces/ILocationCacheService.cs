using WinuiWheaterForecastTray.Models;

namespace WinuiWheaterForecastTray.Services.Interfaces;

/// <summary>
/// Contract for managing cached reverse-geocoded location data.
/// </summary>
public interface ILocationCacheService
{
    /// <summary>
    /// Retrieves cached location data if available.
    /// </summary>
    LocationCacheData? GetCachedLocation();

    /// <summary>
    /// Persists location coordinates and city name to disk cache.
    /// </summary>
    void SaveLocationCache(double latitude, double longitude, string cityName);

    /// <summary>
    /// Clears the cached location file from disk.
    /// </summary>
    void ClearCache();
}
