using System.Collections.Generic;

namespace WinuiWheaterForecastTray.Models;

public class WeatherForecastData
{
    public CurrentWeatherInfo Current { get; set; } = new();
    public List<HourlyForecastItem> HourlyForecast { get; set; } = new();
}
