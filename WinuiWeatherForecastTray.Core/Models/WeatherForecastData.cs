using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinuiWeatherForecastTray.Models;

/// <summary>
/// Aggregate domain data container containing current weather and hourly forecast list.
/// </summary>
public class WeatherForecastData
{
    /// <summary>Current weather info object.</summary>
    public CurrentWeatherInfo Current { get; set; } = new();

    /// <summary>List of upcoming hourly forecast slot items.</summary>
    public List<HourlyForecastItem> HourlyForecast { get; set; } = new();

    /// <summary>UTC timestamp of when this data was fetched from the weather API.</summary>
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// <c>true</c> when this data is a previously cached copy served because a fresh fetch failed.
    /// Never persisted; it is set by the cache layer when the data is read back.
    /// </summary>
    [JsonIgnore]
    public bool IsStale { get; set; }
}
