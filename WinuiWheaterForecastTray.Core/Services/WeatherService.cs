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

        string cityName = await _geocodingService.GetCityNameAsync(lat, lon, cancellationToken).ConfigureAwait(false);
        var dto = await _apiService.GetWeatherDataAsync(lat, lon, cancellationToken).ConfigureAwait(false);
        double aqi = await _airQualityService.GetUsAqiAsync(lat, lon, cancellationToken).ConfigureAwait(false);

        var result = new WeatherForecastData();

        if (dto.Current != null)
        {
            bool isDay = dto.Current.IsDay == 1;
            var (emoji, condition) = _i18nService.GetWeatherCondition(dto.Current.WeatherCode, isDay);

            var summary = _i18nService.FormatSummaryText(
                cityName,
                emoji,
                dto.Current.Temperature2m,
                condition,
                dto.Current.RelativeHumidity2m,
                dto.Current.WindSpeed10m
            );

            // Format date string (e.g., "sexta-feira, 31/07")
            var culture = new CultureInfo(_i18nService.CurrentCulture.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt-BR" : "en-US");
            string formattedDate = DateTime.Now.ToString("dddd, dd/MM", culture);

            // Format sunrise / sunset
            string sunriseTime = dto.Daily?.Sunrise.Count > 0 ? FormatTime(dto.Daily.Sunrise[0]) : "05:55";
            string sunsetTime = dto.Daily?.Sunset.Count > 0 ? FormatTime(dto.Daily.Sunset[0]) : "17:30";

            double uvIndexMax = dto.Daily?.UvIndexMax.Count > 0 ? dto.Daily.UvIndexMax[0] : 3.0;

            result.Current = new CurrentWeatherInfo
            {
                CityName = cityName,
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
                Precipitation = dto.Current.Precipitation,
                AirQualityText = _i18nService.GetAirQualityDescription(aqi),
                UvIndexText = _i18nService.GetUvDescription(uvIndexMax),
                SunriseTime = sunriseTime,
                SunsetTime = sunsetTime,
                IsDay = isDay,
                CustomSummaryText = summary
            };
        }

        if (dto.Hourly != null && dto.Hourly.Time.Count > 0)
        {
            int startIndex = 0;
            if (!string.IsNullOrEmpty(dto.Current?.Time))
            {
                int matchIndex = dto.Hourly.Time.IndexOf(dto.Current.Time);
                if (matchIndex >= 0)
                {
                    startIndex = matchIndex;
                }
            }

            int count = Math.Min(6, dto.Hourly.Time.Count - startIndex);
            for (int i = 0; i < count; i++)
            {
                int idx = startIndex + i;
                string rawTime = dto.Hourly.Time[idx];
                string formattedTime = FormatTime(rawTime);

                double temp = idx < dto.Hourly.Temperature2m.Count ? dto.Hourly.Temperature2m[idx] : 0;
                double rainChance = idx < dto.Hourly.PrecipitationProbability.Count ? dto.Hourly.PrecipitationProbability[idx] : 0;
                int code = idx < dto.Hourly.WeatherCode.Count ? dto.Hourly.WeatherCode[idx] : dto.Current?.WeatherCode ?? 0;
                bool isDay = idx < dto.Hourly.IsDay.Count ? (dto.Hourly.IsDay[idx] == 1) : true;

                var (hourlyEmoji, _) = _i18nService.GetWeatherCondition(code, isDay);

                result.HourlyForecast.Add(new HourlyForecastItem
                {
                    Time = rawTime,
                    FormattedTime = formattedTime,
                    Temperature = temp,
                    Emoji = hourlyEmoji,
                    RainChance = rainChance
                });
            }
        }

        return result;
    }

    private static string FormatTime(string rawTime)
    {
        if (DateTime.TryParse(rawTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
        if (rawTime.Contains('T'))
        {
            var parts = rawTime.Split('T');
            if (parts.Length > 1)
            {
                var timePart = parts[1];
                var subParts = timePart.Split(':');
                if (subParts.Length >= 2)
                {
                    return $"{subParts[0]}:{subParts[1]}";
                }
            }
        }
        return rawTime;
    }
}
