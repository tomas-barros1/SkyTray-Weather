using System.Collections.Generic;

namespace WinuiWheaterForecastTray.Models;

/// <summary>
/// Aggregate domain data container containing current weather and hourly forecast list.
/// </summary>
public class WeatherForecastData
{
    /// <summary>Current weather info object.</summary>
    public CurrentWeatherInfo Current { get; set; } = new();

    /// <summary>List of upcoming hourly forecast slot items.</summary>
    public List<HourlyForecastItem> HourlyForecast { get; set; } = new();
}
