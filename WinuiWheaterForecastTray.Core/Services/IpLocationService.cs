using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class IpLocationService : ILocationService
{
    private static readonly HttpClient DefaultHttpClient = new();
    private readonly HttpClient _httpClient;

    public IpLocationService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    public async Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiResponseDTO>("https://ipapi.co/json/", cancellationToken).ConfigureAwait(false);
            if (response != null && response.Latitude != 0 && response.Longitude != 0)
            {
                return (response.Latitude, response.Longitude);
            }
        }
        catch
        {
            // Try secondary IP geolocation fallback
            try
            {
                var fallback = await _httpClient.GetFromJsonAsync<BigDataCloudIpResponseDTO>("https://api.bigdatacloud.net/data/reverse-geocode-client", cancellationToken).ConfigureAwait(false);
                if (fallback != null && fallback.Latitude != 0 && fallback.Longitude != 0)
                {
                    return (fallback.Latitude, fallback.Longitude);
                }
            }
            catch
            {
                // Silence exception
            }
        }

        return null;
    }
}
