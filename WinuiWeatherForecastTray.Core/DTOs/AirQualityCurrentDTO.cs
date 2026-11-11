using System.Text.Json.Serialization;

namespace WinuiWeatherForecastTray.DTOs;

public class AirQualityCurrentDTO
{
    [JsonPropertyName("us_aqi")]
    public double UsAqi { get; set; }
}
