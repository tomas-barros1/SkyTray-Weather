using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.Constants;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Service implementation for reverse-geocoding coordinates to city/locality names via BigDataCloud API.
/// </summary>
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
            var url = $"{EndpointUrls.BigDataCloudReverseGeocode}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&localityLanguage=en";
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
            DebugLog.Swallowed(typeof(GeocodingService), ex, "Reverse-geocoding failed");
        }

        return null;
    }
}
