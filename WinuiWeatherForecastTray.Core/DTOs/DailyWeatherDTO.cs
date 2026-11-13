using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinuiWeatherForecastTray.DTOs;

public class DailyWeatherDTO
{
    [JsonPropertyName("sunrise")]
    public List<string> Sunrise { get; set; } = new();

    [JsonPropertyName("sunset")]
    public List<string> Sunset { get; set; } = new();

    [JsonPropertyName("uv_index_max")]
    public List<double> UvIndexMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double> TemperatureMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double> TemperatureMin { get; set; } = new();
}
