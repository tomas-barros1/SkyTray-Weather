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

    /// <summary>Set to true by ipapi.co when the request fails (e.g. rate-limited or bad IP).</summary>
    [JsonPropertyName("error")]
    public bool Error { get; set; }

    /// <summary>Human-readable reason when <see cref="Error"/> is true.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
