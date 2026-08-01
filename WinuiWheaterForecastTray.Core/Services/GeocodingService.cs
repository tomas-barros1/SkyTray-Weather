using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class GeocodingService : IGeocodingService
{
    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public GeocodingService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    /// <inheritdoc/>
    public async Task<string?> GetCityNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&localityLanguage=en";
            var result = await _httpClient.GetFromJsonAsync<BigDataCloudGeocodeResponse>(url, cancellationToken).ConfigureAwait(false);

            if (result != null)
            {
                if (!string.IsNullOrWhiteSpace(result.City)) return result.City;
                if (!string.IsNullOrWhiteSpace(result.Locality)) return result.Locality;
                if (!string.IsNullOrWhiteSpace(result.PrincipalSubdivision)) return result.PrincipalSubdivision;
            }
        }
        catch (Exception ex)
        {
            // C-09: return null on failure — do not return a hardcoded city that was never geocoded
            System.Diagnostics.Debug.WriteLine($"[GeocodingService] Reverse-geocoding failed: {ex.Message}");
        }

        return null;
    }
}
