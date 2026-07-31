using System.Text.Json.Serialization;

namespace WinuiWheaterForecastTray.DTOs;

public class BigDataCloudGeocodeResponse
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("locality")]
    public string? Locality { get; set; }

    [JsonPropertyName("principalSubdivision")]
    public string? PrincipalSubdivision { get; set; }
}
