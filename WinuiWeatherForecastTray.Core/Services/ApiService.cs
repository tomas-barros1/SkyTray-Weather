using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Constants;
using WinuiWeatherForecastTray.DTOs;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray.Services;

/// <summary>
/// Service implementation for fetching weather forecast data from the Open-Meteo API.
/// </summary>
public sealed class ApiService : IApiService
{
    private static readonly HttpClient DefaultHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly TimeSpan[] DefaultRetryDelays = { TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(1200) };

    private readonly HttpClient _httpClient;
    private readonly IReadOnlyList<TimeSpan> _retryDelays;

    /// <param name="httpClient">Optional client (tests); defaults to a shared instance with a 5s timeout.</param>
    /// <param name="retryDelays">Delay before each retry of a transient failure; empty disables retries.</param>
    public ApiService(HttpClient? httpClient = null, IReadOnlyList<TimeSpan>? retryDelays = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
        _retryDelays = retryDelays ?? DefaultRetryDelays;
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

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponseDTO>(url, cancellationToken).ConfigureAwait(false);
                return response ?? throw new InvalidOperationException("No weather data returned from Open-Meteo.");
            }
            catch (Exception ex) when (attempt < _retryDelays.Count && IsTransient(ex, cancellationToken))
            {
                DebugLog.Swallowed(typeof(ApiService), ex, $"Transient failure, retry {attempt + 1}/{_retryDelays.Count}");
                await Task.Delay(_retryDelays[attempt], cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Network errors, timeouts, 5xx, 408 and 429 are worth retrying; 4xx and malformed payloads are not.
    /// </summary>
    private static bool IsTransient(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        // HttpClient reports its own timeout as a cancellation; a caller cancellation is not transient.
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: var code } =>
            (int)code >= 500 || code == HttpStatusCode.RequestTimeout || code == HttpStatusCode.TooManyRequests,
        _ => false
    };
}
