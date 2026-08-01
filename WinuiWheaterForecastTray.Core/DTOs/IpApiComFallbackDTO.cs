using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

/// <summary>
/// Response DTO for ip-api.com/json — used as the secondary IP geolocation fallback.
/// Free endpoint, no API key required. Returns "fail" in <see cref="Status"/> on error.
/// </summary>
public class IpApiComFallbackDTO
{
    /// <summary>"success" or "fail".</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
