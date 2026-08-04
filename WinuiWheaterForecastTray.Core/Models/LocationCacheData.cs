using System;

namespace WinuiWheaterForecastTray.Models;

/// <summary>
/// Cached location coordinates and reverse-geocoded city name.
/// </summary>
public class LocationCacheData
{
    /// <summary>Cached latitude coordinate.</summary>
    public double Latitude { get; set; }

    /// <summary>Cached longitude coordinate.</summary>
    public double Longitude { get; set; }

    /// <summary>Cached city or locality name.</summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>Timestamp when the location was cached.</summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
