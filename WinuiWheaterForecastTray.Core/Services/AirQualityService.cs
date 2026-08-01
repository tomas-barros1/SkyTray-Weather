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
/// Service implementation for fetching US Air Quality Index (AQI) data from Open-Meteo Air Quality API.
/// </summary>
public sealed class AirQualityService : IAirQualityService
{
    // R-05: Named constant for fallback AQI
    private const double DefaultUsAqi = 42.0; // Good / Razoável AQI fallback

    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public AirQualityService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    /// <inheritdoc/>
    public async Task<double> GetUsAqiAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{EndpointUrls.OpenMeteoAirQuality}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current=us_aqi";
            var response = await _httpClient.GetFromJsonAsync<AirQualityDTO>(url, cancellationToken).ConfigureAwait(false);
            if (response?.Current != null)
            {
                return response.Current.UsAqi;
            }
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(AirQualityService), ex, "AQI fetch failed, defaulting to 42.0");
        }

        return DefaultUsAqi;
    }
}
