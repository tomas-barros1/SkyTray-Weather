using System;
using System.IO;
using System.Text.Json;
using WinuiWheaterForecastTray.Models;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Service implementation for persisting reverse-geocoded city location cache to JSON file.
/// </summary>
public sealed class LocationCacheService : ILocationCacheService
{
    private readonly string _cacheFilePath;

    public LocationCacheService(string? customFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _cacheFilePath = customFilePath;
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(localAppData, "SkyTrayWeather");
            _cacheFilePath = Path.Combine(folder, "location_cache.json");
        }
    }

    /// <inheritdoc/>
    public LocationCacheData? GetCachedLocation()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                return JsonSerializer.Deserialize<LocationCacheData>(json);
            }
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(LocationCacheService), ex, "Failed to read location cache");
        }

        return null;
    }

    /// <inheritdoc/>
    public void SaveLocationCache(double latitude, double longitude, string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            return;

        try
        {
            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var cacheData = new LocationCacheData
            {
                Latitude = latitude,
                Longitude = longitude,
                CityName = cityName,
                CachedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(LocationCacheService), ex, "Failed to write location cache");
        }
    }

    /// <inheritdoc/>
    public void ClearCache()
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
            DebugLog.Swallowed(typeof(LocationCacheService), ex, "Failed to clear location cache");
        }
    }
}
