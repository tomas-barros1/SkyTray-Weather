using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.Models;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class WeatherService : IWeatherService
{
    private const double DefaultLatitude = -23.5505;
    private const double DefaultLongitude = -46.6333;

    // P-04: cached CultureInfo instances — avoid allocating per refresh-timer call
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private readonly IApiService _apiService;
    private readonly ILocationService _locationService;
    private readonly IGeocodingService _geocodingService;
    private readonly ILocationService _ipLocationService;
    private readonly II18nService _i18nService;
    private readonly IAirQualityService _airQualityService;

    public WeatherService(
        IApiService apiService,
        ILocationService locationService,
        IGeocodingService geocodingService,
        ILocationService? ipLocationService = null,
        II18nService? i18nService = null,
        IAirQualityService? airQualityService = null)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
        _ipLocationService = ipLocationService ?? new IpLocationService();
        _i18nService = i18nService ?? new I18nService();
        _airQualityService = airQualityService ?? new AirQualityService();
    }

    public async Task<WeatherForecastData> GetForecastAsync(double? customLat = null, double? customLon = null, CancellationToken cancellationToken = default)
    {
        double lat = customLat ?? DefaultLatitude;
        double lon = customLon ?? DefaultLongitude;

        if (customLat == null || customLon == null)
        {
            var userLocation = await _locationService.GetLocationAsync(cancellationToken).ConfigureAwait(false);
            if (userLocation.HasValue)
            {
                lat = userLocation.Value.Latitude;
                lon = userLocation.Value.Longitude;
            }
            else
            {
                var ipLocation = await _ipLocationService.GetLocationAsync(cancellationToken).ConfigureAwait(false);
                if (ipLocation.HasValue)
                {
                    lat = ipLocation.Value.Latitude;
                    lon = ipLocation.Value.Longitude;
                }
            }
        }

        // P-01: fan out three independent HTTP calls concurrently — worst-case latency ~5s instead of ~15s
        var weatherTask = _apiService.GetWeatherDataAsync(lat, lon, cancellationToken);
        var aqiTask    = _airQualityService.GetUsAqiAsync(lat, lon, cancellationToken);
        var cityTask   = _geocodingService.GetCityNameAsync(lat, lon, cancellationToken);
        await Task.WhenAll(weatherTask, aqiTask, cityTask).ConfigureAwait(false);

        var dto      = weatherTask.Result;
        double aqi   = aqiTask.Result;
        string? cityName = cityTask.Result;

        // C-05: throw instead of silently returning defaulted data when the API returns nothing
        if (dto.Current is null)
            throw new InvalidOperationException("No current weather data returned from Open-Meteo.");

        // C-06: single source of truth for 'now' — derived from API response time, not local clock
        DateTime now = DateTime.TryParse(dto.Current.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedNow)
            ? parsedNow
            : DateTime.Now;

        // C-09: cityName is now string? — use empty string as display fallback
        string displayCityName = cityName ?? string.Empty;

        bool isDay = dto.Current.IsDay == 1;
        var (emoji, condition) = _i18nService.GetWeatherCondition(dto.Current.WeatherCode, isDay);

        var summary = _i18nService.FormatSummaryText(
            displayCityName, emoji,
            dto.Current.Temperature2m, condition,
            dto.Current.RelativeHumidity2m, dto.Current.WindSpeed10m);

        // P-04: use cached CultureInfo
        CultureInfo dateCulture = _i18nService.CurrentCulture.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
            ? PtBr : EnUs;
        string formattedDate = now.ToString("dddd, dd/MM", dateCulture);

        string sunriseTime = dto.Daily?.Sunrise.Count > 0 ? FormatTime(dto.Daily.Sunrise[0]) : "05:55";
        string sunsetTime  = dto.Daily?.Sunset.Count  > 0 ? FormatTime(dto.Daily.Sunset[0])  : "17:30";
        double uvIndexMax  = dto.Daily?.UvIndexMax.Count > 0 ? dto.Daily.UvIndexMax[0] : 3.0;

        // P-03: single pass over hourly data — finds both precip-prob slot and forecast start index
        double precipProb = 0.0;
        int startIndex = 0;
        DateTime nextHourTarget = now.Minute > 0 ? now.AddHours(1) : now; // e.g. 13:56 → start at 14:00

        if (dto.Hourly != null)
        {
            bool precipFound = false;
            bool startFound  = false;

            for (int i = 0; i < dto.Hourly.Time.Count; i++)
            {
                if (!DateTime.TryParse(dto.Hourly.Time[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                    continue;

                // Capture precipitation probability for the current hour
                if (!precipFound && t.Date == now.Date && t.Hour == now.Hour)
                {
                    precipProb = i < dto.Hourly.PrecipitationProbability.Count
                        ? dto.Hourly.PrecipitationProbability[i]
                        : 0.0;
                    precipFound = true;
                }

                // Capture the first hourly slot at or after nextHourTarget for the forecast strip
                if (!startFound && (t.Date > nextHourTarget.Date
                    || (t.Date == nextHourTarget.Date && t.Hour >= nextHourTarget.Hour)))
                {
                    startIndex = i;
                    startFound = true;
                }

                if (precipFound && startFound) break; // both captured — no need to continue
            }
        }

        var result = new WeatherForecastData
        {
            Current = new CurrentWeatherInfo
            {
                CityName              = displayCityName,
                DateString            = formattedDate,
                Temperature           = dto.Current.Temperature2m,
                ApparentTemperature   = dto.Current.ApparentTemperature,
                WeatherCode           = dto.Current.WeatherCode,
                ConditionText         = condition,
                Emoji                 = emoji,
                Humidity              = dto.Current.RelativeHumidity2m,
                WindSpeed             = dto.Current.WindSpeed10m,
                CloudCover            = dto.Current.CloudCover,
                SurfacePressure       = dto.Current.SurfacePressure,
                PrecipitationProbability = precipProb,
                AirQualityText        = _i18nService.GetAirQualityDescription(aqi),
                UvIndexText           = _i18nService.GetUvDescription(uvIndexMax),
                SunriseTime           = sunriseTime,
                SunsetTime            = sunsetTime,
                IsDay                 = isDay,
                CustomSummaryText     = summary
            }
        };

        if (dto.Hourly != null && dto.Hourly.Time.Count > 0)
        {
            int count = Math.Min(6, dto.Hourly.Time.Count - startIndex);
            for (int i = 0; i < count; i++)
            {
                int idx = startIndex + i;
                string rawTime      = dto.Hourly.Time[idx];
                double temp         = idx < dto.Hourly.Temperature2m.Count ? dto.Hourly.Temperature2m[idx] : 0;
                double rainChance   = idx < dto.Hourly.PrecipitationProbability.Count ? dto.Hourly.PrecipitationProbability[idx] : 0;
                int code            = idx < dto.Hourly.WeatherCode.Count ? dto.Hourly.WeatherCode[idx] : dto.Current.WeatherCode;
                bool isDayHourly    = idx < dto.Hourly.IsDay.Count ? (dto.Hourly.IsDay[idx] == 1) : true;

                var (hourlyEmoji, _) = _i18nService.GetWeatherCondition(code, isDayHourly);

                result.HourlyForecast.Add(new HourlyForecastItem
                {
                    Time          = rawTime,
                    FormattedTime = FormatTime(rawTime),
                    Temperature   = temp,
                    Emoji         = hourlyEmoji,
                    RainChance    = rainChance
                });
            }
        }

        return result;
    }

    private static string FormatTime(string rawTime)
    {
        if (DateTime.TryParse(rawTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (rawTime.Contains('T'))
        {
            var parts = rawTime.Split('T');
            if (parts.Length > 1)
            {
                var sub = parts[1].Split(':');
                if (sub.Length >= 2) return $"{sub[0]}:{sub[1]}";
            }
        }
        return rawTime;
    }
}
