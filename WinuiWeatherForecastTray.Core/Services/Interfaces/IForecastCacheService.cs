using WinuiWeatherForecastTray.Models;

namespace WinuiWeatherForecastTray.Services.Interfaces;

/// <summary>
/// Contract for persisting the last successfully fetched forecast so it can be shown while offline.
/// </summary>
public interface IForecastCacheService
{
    /// <summary>Returns the last saved forecast, or <c>null</c> if none exists or it cannot be read.</summary>
    WeatherForecastData? Load();

    /// <summary>Persists <paramref name="data"/> as the latest known forecast.</summary>
    void Save(WeatherForecastData data);

    /// <summary>Deletes the saved forecast, if any.</summary>
    void Clear();
}
