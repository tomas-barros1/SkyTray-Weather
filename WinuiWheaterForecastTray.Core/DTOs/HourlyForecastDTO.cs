using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

public class HourlyForecastDTO
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature2m { get; set; } = new();

    [JsonPropertyName("precipitation_probability")]
    public List<double> PrecipitationProbability { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = new();

    [JsonPropertyName("is_day")]
    public List<int> IsDay { get; set; } = new();
}
