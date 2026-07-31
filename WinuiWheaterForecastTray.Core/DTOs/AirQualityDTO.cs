using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

public class AirQualityDTO
{
    [JsonPropertyName("current")]
    public AirQualityCurrentDTO? Current { get; set; }
}
