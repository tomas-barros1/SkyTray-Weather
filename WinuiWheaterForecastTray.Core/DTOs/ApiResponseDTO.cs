using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

public class ApiResponseDTO
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("current")]
    public CurrentWeatherDTO? Current { get; set; }

    [JsonPropertyName("hourly")]
    public HourlyForecastDTO? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public DailyWeatherDTO? Daily { get; set; }
}
