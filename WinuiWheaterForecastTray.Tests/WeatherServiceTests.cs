using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WinuiWheaterForecastTray.DTOs;
using WinuiWheaterForecastTray.Services;
using WinuiWheaterForecastTray.Services.Interfaces;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class WeatherServiceTests
{
    [Fact]
    public async Task GetForecastAsync_TransformsDtoToDomainModel_Correctly()
    {
        // Arrange
        var mockApi = new Mock<IApiService>();
        var mockLocation = new Mock<ILocationService>();
        var mockGeocoding = new Mock<IGeocodingService>();
        var englishI18n = new I18nService("en-US");

        mockLocation.Setup(l => l.GetLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((-23.5505, -46.6333));

        mockGeocoding.Setup(g => g.GetCityNameAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)"São Paulo");

        var dto = new ApiResponseDTO
        {
            Latitude = -23.5505,
            Longitude = -46.6333,
            Current = new CurrentWeatherDTO
            {
                Time = "2026-07-31T13:00",
                Temperature2m = 22.0,
                ApparentTemperature = 24.0,
                WeatherCode = 0,
                RelativeHumidity2m = 68.0,
                WindSpeed10m = 12.0,
                IsDay = 1
            },
            Hourly = new HourlyForecastDTO
            {
                Time = new List<string>
                {
                    "2026-07-31T13:00",
                    "2026-07-31T14:00",
                    "2026-07-31T15:00",
                    "2026-07-31T16:00",
                    "2026-07-31T17:00",
                    "2026-07-31T18:00",
                    "2026-07-31T19:00"
                },
                Temperature2m = new List<double> { 23.0, 24.0, 22.0, 21.0, 21.0, 20.0, 19.0 },
                PrecipitationProbability = new List<double> { 0, 10, 80, 75, 20, 0, 0 },
                WeatherCode = new List<int> { 0, 1, 61, 61, 2, 0, 0 },
                IsDay = new List<int> { 1, 1, 1, 1, 1, 0, 0 }
            }
        };

        mockApi.Setup(a => a.GetWeatherDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var weatherService = new WeatherService(mockApi.Object, mockLocation.Object, mockGeocoding.Object, null, englishI18n);

        // Act
        var forecastData = await weatherService.GetForecastAsync();

        // Assert
        forecastData.Should().NotBeNull();
        forecastData.Current.CityName.Should().Be("São Paulo");
        forecastData.Current.Temperature.Should().Be(22.0);
        forecastData.Current.ApparentTemperature.Should().Be(24.0);
        forecastData.Current.Humidity.Should().Be(68.0);
        forecastData.Current.WindSpeed.Should().Be(12.0);
        forecastData.Current.Emoji.Should().Be("☀️");
        forecastData.Current.SummaryText.Should().Contain("São Paulo")
            .And.Contain("22°C")
            .And.Contain("68% humidity")
            .And.Contain("12 km/h wind");

        forecastData.HourlyForecast.Should().HaveCount(6);
        forecastData.HourlyForecast[0].FormattedTime.Should().Be("13:00");
        forecastData.HourlyForecast[0].Emoji.Should().Be("☀️");
        forecastData.HourlyForecast[0].DisplayTemperature.Should().Be("23°");

        forecastData.HourlyForecast[5].FormattedTime.Should().Be("18:00");
        forecastData.HourlyForecast[5].Emoji.Should().Be("🌙");
        forecastData.HourlyForecast[5].DisplayTemperature.Should().Be("20°");

        // The current hour is 13:00 — precipitation probability for that slot is 0%
        forecastData.Current.PrecipitationProbability.Should().Be(0,
            because: "PrecipitationProbability is sourced from hourly[0] (13:00) which has value 0");

        // Hourly slot at 15:00 (index=2) has 80% rain chance
        forecastData.HourlyForecast[2].RainChance.Should().Be(80);
    }

    // C-05: WeatherService must throw when dto.Current is null instead of silently returning defaults
    [Fact]
    public async Task GetForecastAsync_NullCurrentWeather_ThrowsInvalidOperationException()
    {
        var mockApi = new Mock<IApiService>();
        var mockLocation = new Mock<ILocationService>();
        var mockGeocoding = new Mock<IGeocodingService>();
        var i18n = new I18nService("en-US");

        mockLocation.Setup(l => l.GetLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((-23.5505, -46.6333));
        mockGeocoding.Setup(g => g.GetCityNameAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // API returns a DTO with null Current
        var dto = new ApiResponseDTO { Latitude = -23.5505, Longitude = -46.6333, Current = null };
        mockApi.Setup(a => a.GetWeatherDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var service = new WeatherService(mockApi.Object, mockLocation.Object, mockGeocoding.Object, null, i18n);

        Func<Task> act = () => service.GetForecastAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No current weather data*", because: "silent default is worse than a clear failure signal");
    }

    // P-01: all three HTTP calls (weather, AQI, geocoding) must be issued concurrently.
    // With a 2s artificial delay on each, total time must be closer to 2s than 6s.
    [Fact]
    public async Task GetForecastAsync_ThreeServices_AreCalledConcurrently()
    {
        const int DelayMs = 2000;

        var mockApi = new Mock<IApiService>();
        var mockLocation = new Mock<ILocationService>();
        var mockGeocoding = new Mock<IGeocodingService>();
        var mockAqi = new Mock<IAirQualityService>();
        var i18n = new I18nService("en-US");

        mockLocation.Setup(l => l.GetLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((-23.5505, -46.6333));

        mockGeocoding.Setup(g => g.GetCityNameAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(async (double _, double _, CancellationToken ct) =>
            {
                await Task.Delay(DelayMs, ct);
                return (string?)"São Paulo";
            });

        mockAqi.Setup(a => a.GetUsAqiAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(async (double _, double _, CancellationToken ct) =>
            {
                await Task.Delay(DelayMs, ct);
                return 25.0;
            });

        var dto = new ApiResponseDTO
        {
            Latitude = -23.5505,
            Longitude = -46.6333,
            Current = new CurrentWeatherDTO
            {
                Time = "2026-07-31T13:00",
                Temperature2m = 22.0,
                ApparentTemperature = 24.0,
                WeatherCode = 0,
                RelativeHumidity2m = 68.0,
                WindSpeed10m = 12.0,
                IsDay = 1
            },
            Hourly = new HourlyForecastDTO
            {
                Time = new System.Collections.Generic.List<string>
                    { "2026-07-31T13:00","2026-07-31T14:00","2026-07-31T15:00",
                      "2026-07-31T16:00","2026-07-31T17:00","2026-07-31T18:00","2026-07-31T19:00" },
                Temperature2m = new System.Collections.Generic.List<double> { 23, 24, 22, 21, 21, 20, 19 },
                PrecipitationProbability = new System.Collections.Generic.List<double> { 0, 10, 80, 75, 20, 0, 0 },
                WeatherCode = new System.Collections.Generic.List<int> { 0, 1, 61, 61, 2, 0, 0 },
                IsDay = new System.Collections.Generic.List<int> { 1, 1, 1, 1, 1, 0, 0 }
            }
        };

        mockApi.Setup(a => a.GetWeatherDataAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(async (double _, double _, CancellationToken ct) =>
            {
                await Task.Delay(DelayMs, ct);
                return dto;
            });

        var service = new WeatherService(mockApi.Object, mockLocation.Object, mockGeocoding.Object,
            null, i18n, mockAqi.Object);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await service.GetForecastAsync();
        sw.Stop();

        result.Should().NotBeNull();
        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(DelayMs * 2.5,
            because: "three concurrent 2s tasks must complete in ~2s, not ~6s");
    }
}
