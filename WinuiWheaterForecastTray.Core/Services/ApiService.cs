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
/// Service implementation for fetching weather forecast data from the Open-Meteo API.
/// </summary>
public sealed class ApiService : IApiService
{
    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    /// <inheritdoc/>
    public async Task<ApiResponseDTO> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"{EndpointUrls.OpenMeteoForecast}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                  "&current=temperature_2m,apparent_temperature,weather_code,relative_humidity_2m,wind_speed_10m,cloud_cover,pressure_msl,precipitation,is_day" +
                  "&hourly=temperature_2m,precipitation_probability,weather_code,is_day" +
                  "&daily=sunrise,sunset,uv_index_max" +
                  "&timezone=auto";

        var response = await _httpClient.GetFromJsonAsync<ApiResponseDTO>(url, cancellationToken).ConfigureAwait(false);
        return response ?? throw new InvalidOperationException("No weather data returned from Open-Meteo.");
    }
}
