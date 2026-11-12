using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Constants;
using WinuiWeatherForecastTray.DTOs;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray.Services;

/// <summary>
/// Service implementation for IP-based geolocation using ipapi.co with ipwho.is as HTTPS fallback.
/// Coordinates from either provider are validated before being returned (and therefore before they can
/// be cached): they come from the network and are treated as untrusted input.
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
                var coordinates = ValidateCoordinates(response.Latitude, response.Longitude);
                if (coordinates.HasValue)
                    return coordinates;

                System.Diagnostics.Debug.WriteLine("[IpLocationService] ipapi.co returned missing or out-of-range coordinates");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[IpLocationService] ipapi.co reported error: {response?.Reason ?? "unknown"}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IpLocationService] ipapi.co request failed: {ex.Message}");
        }

        // Fallback: ipwho.is (HTTPS)
        try
        {
            var fallback = await _httpClient
                .GetFromJsonAsync<IpWhoIsDTO>(EndpointUrls.IpWhoIsJson, cancellationToken)
                .ConfigureAwait(false);

            if (fallback is { Success: true })
            {
                var coordinates = ValidateCoordinates(fallback.Latitude, fallback.Longitude);
                if (coordinates.HasValue)
                    return coordinates;

                System.Diagnostics.Debug.WriteLine("[IpLocationService] ipwho.is returned missing or out-of-range coordinates");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[IpLocationService] ipwho.is fallback was not successful: {fallback?.Message ?? "unknown"}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IpLocationService] ipwho.is fallback failed: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Accepts only real WGS84 coordinates. (0, 0) is accepted when explicitly returned (it is a valid
    /// point), but a missing field (null), NaN or an out-of-range value is rejected.
    /// </summary>
    private static (double Latitude, double Longitude)? ValidateCoordinates(double? latitude, double? longitude)
    {
        if (latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180)
            return (latitude.Value, longitude.Value);

        return null;
    }
}
