using System.Text.Json.Serialization;

namespace WinuiWeatherForecastTray.DTOs;

/// <summary>
/// Response DTO for https://ipwho.is/ — the secondary IP geolocation provider (HTTPS, no API key).
/// <see cref="Success"/> is false on error, with the reason in <see cref="Message"/>.
/// </summary>
public class IpWhoIsDTO
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Nullable on purpose: a missing field must not be mistaken for latitude 0.</summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    /// <summary>Nullable on purpose: a missing field must not be mistaken for longitude 0.</summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>Only present when <see cref="Success"/> is false.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
