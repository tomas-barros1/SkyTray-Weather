using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.Constants;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Service implementation for IP-based geolocation using ipapi.co with ip-api.com fallback.
/// </summary>
public sealed class IpLocationService : ILocationService
{
    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public IpLocationService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    /// <inheritdoc/>
    public async Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        // Primary: ipapi.co
        try
        {
            var response = await _httpClient
                .GetFromJsonAsync<IpApiResponseDTO>(EndpointUrls.IpApiCoJson, cancellationToken)
                .ConfigureAwait(false);

            if (response is { Error: false })
            {
                return (response.Latitude, response.Longitude);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[IpLocationService] ipapi.co reported error: {response?.Reason ?? "unknown"}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IpLocationService] ipapi.co request failed: {ex.Message}");
        }

        // Fallback: ip-api.com
        try
        {
            var fallback = await _httpClient
                .GetFromJsonAsync<IpApiComFallbackDTO>(EndpointUrls.IpApiComJson, cancellationToken)
                .ConfigureAwait(false);

            if (fallback is { Status: "success" })
            {
                return (fallback.Lat, fallback.Lon);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[IpLocationService] ip-api.com fallback returned non-success status: {fallback?.Status}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IpLocationService] ip-api.com fallback failed: {ex.Message}");
        }

        return null;
    }
}
