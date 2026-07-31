using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class ApiService : IApiService
{
    private const string ForecastBaseUrl = "https://api.open-meteo.com/v1/forecast";
    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    public async Task<ApiResponseDTO> GetWeatherDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"{ForecastBaseUrl}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                  "&current=temperature_2m,apparent_temperature,weather_code,relative_humidity_2m,wind_speed_10m,cloud_cover,surface_pressure,precipitation,is_day" +
                  "&hourly=temperature_2m,precipitation_probability,weather_code,is_day" +
                  "&daily=sunrise,sunset,uv_index_max" +
                  "&timezone=auto";

        var response = await _httpClient.GetFromJsonAsync<ApiResponseDTO>(url, cancellationToken).ConfigureAwait(false);
        return response ?? throw new InvalidOperationException("No weather data returned from Open-Meteo.");
    }
}
