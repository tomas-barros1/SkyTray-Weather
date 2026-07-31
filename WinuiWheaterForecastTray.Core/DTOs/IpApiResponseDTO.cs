using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

public class IpApiResponseDTO
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }
}
