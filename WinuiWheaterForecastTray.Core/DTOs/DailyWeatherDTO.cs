using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

public class DailyWeatherDTO
{
    [JsonPropertyName("sunrise")]
    public List<string> Sunrise { get; set; } = new();

    [JsonPropertyName("sunset")]
    public List<string> Sunset { get; set; } = new();

    [JsonPropertyName("uv_index_max")]
    public List<double> UvIndexMax { get; set; } = new();
}
