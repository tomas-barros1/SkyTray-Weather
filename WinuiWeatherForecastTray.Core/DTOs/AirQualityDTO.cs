using System.Text.Json.Serialization;

namespace WinuiWeatherForecastTray.DTOs;

public class AirQualityDTO
{
    [JsonPropertyName("current")]
    public AirQualityCurrentDTO? Current { get; set; }
}
