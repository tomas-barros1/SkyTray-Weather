using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class AirQualityService : IAirQualityService
{
    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public AirQualityService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    public async Task<double> GetUsAqiAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current=us_aqi";
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

        return 42.0; // Default Good / Razoável AQI
    }
}
