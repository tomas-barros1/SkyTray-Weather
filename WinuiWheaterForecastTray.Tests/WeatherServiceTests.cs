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
            .ReturnsAsync("São Paulo");

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
    }
}
