using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Models;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Orchestrator service responsible for aggregating weather, geocoding, and air quality data into unified domain models.
/// </summary>
public sealed class WeatherService : IWeatherService
{
    private const double DefaultLatitude = -23.5505;
    private const double DefaultLongitude = -46.6333;

    // R-01 / R-05: Named constants for fallback values
    private const string DefaultSunriseTime = "05:55";
    private const string DefaultSunsetTime = "17:30";
    private const double DefaultUvIndexMax = 3.0;
    private const int HourlyForecastSlots = 6;

    private readonly IApiService _apiService;
    private readonly IGeocodingService _geocodingService;
    private readonly II18nService _i18nService;
    private readonly IAirQualityService _airQualityService;

    private readonly IReadOnlyList<ILocationService> _locationProviders;

    public WeatherService(
        IApiService apiService,
        ILocationService locationService,
        IGeocodingService geocodingService,
        ILocationService? ipLocationService = null,
        II18nService? i18nService = null,
        IAirQualityService? airQualityService = null)
        : this(
            apiService,
            geocodingService,
            CreateProviderChain(locationService, ipLocationService),
            i18nService,
            airQualityService)
    { }

    public WeatherService(
        IApiService apiService,
        IGeocodingService geocodingService,
        IEnumerable<ILocationService> locationProviders,
        II18nService? i18nService = null,
        IAirQualityService? airQualityService = null)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
        _locationProviders = new List<ILocationService>(locationProviders ?? throw new ArgumentNullException(nameof(locationProviders))).AsReadOnly();
        _i18nService = i18nService ?? new I18nService();
        _airQualityService = airQualityService ?? new AirQualityService();
    }

    private static IEnumerable<ILocationService> CreateProviderChain(ILocationService primary, ILocationService? secondary)
    {
        if (primary != null) yield return primary;
        yield return secondary ?? new IpLocationService();
    }

    /// <inheritdoc/>
    public async Task<WeatherForecastData> GetForecastAsync(double? customLat = null, double? customLon = null, CancellationToken cancellationToken = default)
    {
        var (lat, lon) = await ResolveCoordinatesAsync(customLat, customLon, cancellationToken).ConfigureAwait(false);
        var (dto, aqi, cityName) = await FetchAllAsync(lat, lon, cancellationToken).ConfigureAwait(false);

        return BuildForecastData(dto, aqi, cityName);
    }

    private async Task<(double Latitude, double Longitude)> ResolveCoordinatesAsync(double? customLat, double? customLon, CancellationToken cancellationToken)
    {
        if (customLat.HasValue && customLon.HasValue)
            return (customLat.Value, customLon.Value);

        foreach (var provider in _locationProviders)
        {
            var location = await provider.GetLocationAsync(cancellationToken).ConfigureAwait(false);
            if (location.HasValue)
                return location.Value;
        }

        return (DefaultLatitude, DefaultLongitude);
    }

    private async Task<(ApiResponseDTO dto, double aqi, string? cityName)> FetchAllAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        var weatherTask = _apiService.GetWeatherDataAsync(lat, lon, cancellationToken);
        var aqiTask = _airQualityService.GetUsAqiAsync(lat, lon, cancellationToken);
        var cityTask = _geocodingService.GetCityNameAsync(lat, lon, cancellationToken);

        await Task.WhenAll(weatherTask, aqiTask, cityTask).ConfigureAwait(false);

        return (weatherTask.Result, aqiTask.Result, cityTask.Result);
    }

    private WeatherForecastData BuildForecastData(ApiResponseDTO dto, double aqi, string? cityName)
    {
        if (dto.Current is null)
            throw new InvalidOperationException("No current weather data returned from Open-Meteo.");

        DateTime now = DateTime.TryParse(dto.Current.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedNow)
            ? parsedNow
            : DateTime.Now;

        string displayCityName = cityName ?? string.Empty;

        var (precipProb, startIndex) = ExtractHourlyMetadata(dto.Hourly, now);

        return new WeatherForecastData
        {
            Current = BuildCurrentWeatherInfo(dto, aqi, displayCityName, now, precipProb),
            HourlyForecast = BuildHourlyForecastItems(dto, startIndex)
        };
    }

    private CurrentWeatherInfo BuildCurrentWeatherInfo(ApiResponseDTO dto, double aqi, string displayCityName, DateTime now, double precipProb)
    {
        bool isDay = dto.Current!.IsDay == 1;
        var (emoji, condition) = _i18nService.GetWeatherCondition(dto.Current.WeatherCode, isDay);

        var summary = _i18nService.FormatSummaryText(
            displayCityName, emoji,
            dto.Current.Temperature2m, condition,
            dto.Current.RelativeHumidity2m, dto.Current.WindSpeed10m);

        string formattedDate = now.ToString("dddd, dd/MM", _i18nService.CurrentFormatProvider);

        string sunriseTime = dto.Daily?.Sunrise.Count > 0 ? FormatTime(dto.Daily.Sunrise[0]) : DefaultSunriseTime;
        string sunsetTime = dto.Daily?.Sunset.Count > 0 ? FormatTime(dto.Daily.Sunset[0]) : DefaultSunsetTime;
        double uvIndexMax = dto.Daily?.UvIndexMax.Count > 0 ? dto.Daily.UvIndexMax[0] : DefaultUvIndexMax;

        return new CurrentWeatherInfo
        {
            CityName = displayCityName,
            DateString = formattedDate,
            Temperature = dto.Current.Temperature2m,
            ApparentTemperature = dto.Current.ApparentTemperature,
            WeatherCode = dto.Current.WeatherCode,
            ConditionText = condition,
            Emoji = emoji,
            Humidity = dto.Current.RelativeHumidity2m,
            WindSpeed = dto.Current.WindSpeed10m,
            CloudCover = dto.Current.CloudCover,
            SurfacePressure = dto.Current.SurfacePressure,
            PrecipitationProbability = precipProb,
            AirQualityText = _i18nService.GetAirQualityDescription(aqi),
            UvIndexText = _i18nService.GetUvDescription(uvIndexMax),
            SunriseTime = sunriseTime,
            SunsetTime = sunsetTime,
            IsDay = isDay,
            CustomSummaryText = summary
        };
    }

    private static (double PrecipProb, int StartIndex) ExtractHourlyMetadata(HourlyForecastDTO? hourly, DateTime now)
    {
        double precipProb = 0.0;
        int startIndex = 0;
        DateTime nextHourTarget = now.Minute > 0 ? now.AddHours(1) : now;

        if (hourly != null)
        {
            bool precipFound = false;
            bool startFound = false;

            for (int i = 0; i < hourly.Time.Count; i++)
            {
                if (!DateTime.TryParse(hourly.Time[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                    continue;

                if (!precipFound && t.Date == now.Date && t.Hour == now.Hour)
                {
                    precipProb = i < hourly.PrecipitationProbability.Count ? hourly.PrecipitationProbability[i] : 0.0;
                    precipFound = true;
                }

                if (!startFound && (t.Date > nextHourTarget.Date || (t.Date == nextHourTarget.Date && t.Hour >= nextHourTarget.Hour)))
                {
                    startIndex = i;
                    startFound = true;
                }

                if (precipFound && startFound) break;
            }
        }

        return (precipProb, startIndex);
    }

    private List<HourlyForecastItem> BuildHourlyForecastItems(ApiResponseDTO dto, int startIndex)
    {
        var items = new List<HourlyForecastItem>();
        if (dto.Hourly == null || dto.Hourly.Time.Count == 0)
            return items;

        int count = Math.Min(HourlyForecastSlots, dto.Hourly.Time.Count - startIndex);
        for (int i = 0; i < count; i++)
        {
            int idx = startIndex + i;
            string rawTime = dto.Hourly.Time[idx];
            double temp = idx < dto.Hourly.Temperature2m.Count ? dto.Hourly.Temperature2m[idx] : 0;
            double rainChance = idx < dto.Hourly.PrecipitationProbability.Count ? dto.Hourly.PrecipitationProbability[idx] : 0;
            int code = idx < dto.Hourly.WeatherCode.Count ? dto.Hourly.WeatherCode[idx] : dto.Current?.WeatherCode ?? 0;
            bool isDayHourly = idx < dto.Hourly.IsDay.Count ? (dto.Hourly.IsDay[idx] == 1) : true;

            var (hourlyEmoji, _) = _i18nService.GetWeatherCondition(code, isDayHourly);

            items.Add(new HourlyForecastItem
            {
                Time = rawTime,
                FormattedTime = FormatTime(rawTime),
                Temperature = temp,
                Emoji = hourlyEmoji,
                RainChance = rainChance
            });
        }

        return items;
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
