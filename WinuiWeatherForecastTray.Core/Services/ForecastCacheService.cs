using System;
using System.IO;
using System.Text.Json;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray.Services;

/// <summary>
/// Persists the latest forecast as JSON under %LocalAppData%\SkyTrayWeather so the app can show
/// stale-but-useful data when the network is unavailable (e.g. right after boot).
/// </summary>
public sealed class ForecastCacheService : IForecastCacheService
{
    private readonly string _cacheFilePath;

    public ForecastCacheService(string? customFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _cacheFilePath = customFilePath;
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheFilePath = Path.Combine(localAppData, "SkyTrayWeather", "forecast_cache.json");
        }
    }

    /// <inheritdoc/>
    public WeatherForecastData? Load()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<WeatherForecastData>(json);
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(ForecastCacheService), ex, "Failed to read forecast cache");
            return null;
        }
    }

    /// <inheritdoc/>
    public void Save(WeatherForecastData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Write to a temp file and move it over the target so a crash mid-write
            // can never leave a truncated (unreadable) cache behind.
            var tempPath = _cacheFilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(data));
            File.Move(tempPath, _cacheFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(ForecastCacheService), ex, "Failed to write forecast cache");
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
            }
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(ForecastCacheService), ex, "Failed to clear forecast cache");
        }
    }
}
