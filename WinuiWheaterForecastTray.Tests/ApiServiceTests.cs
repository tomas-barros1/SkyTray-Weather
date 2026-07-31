using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class ApiServiceTests
{
    [Fact]
    public async Task GetWeatherDataAsync_ValidResponse_DeserializesCorrectly()
    {
        // Arrange
        var jsonResponse = @"{
            ""latitude"": -23.55,
            ""longitude"": -46.63,
            ""timezone"": ""America/Sao_Paulo"",
            ""current"": {
                ""time"": ""2026-07-31T12:00"",
                ""temperature_2m"": 22.4,
                ""apparent_temperature"": 24.1,
                ""weather_code"": 0,
                ""relative_humidity_2m"": 68.0,
                ""wind_speed_10m"": 12.0,
                ""is_day"": 1
            },
            ""hourly"": {
                ""time"": [""2026-07-31T12:00"", ""2026-07-31T13:00""],
                ""temperature_2m"": [22.4, 23.0],
                ""precipitation_probability"": [10.0, 20.0],
                ""weather_code"": [0, 1],
                ""is_day"": [1, 1]
            }
        }";

        var mockHandler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(mockHandler);
        var apiService = new ApiService(httpClient);

        // Act
        var result = await apiService.GetWeatherDataAsync(-23.55, -46.63);

        // Assert
        result.Should().NotBeNull();
        result.Latitude.Should().Be(-23.55);
        result.Longitude.Should().Be(-46.63);
        result.Current.Should().NotBeNull();
        result.Current!.Temperature2m.Should().Be(22.4);
        result.Current.ApparentTemperature.Should().Be(24.1);
        result.Current.WeatherCode.Should().Be(0);
        result.Current.RelativeHumidity2m.Should().Be(68.0);
        result.Current.WindSpeed10m.Should().Be(12.0);
        result.Current.IsDay.Should().Be(1);

        result.Hourly.Should().NotBeNull();
        result.Hourly!.Time.Should().HaveCount(2);
        result.Hourly.Temperature2m.Should().ContainInOrder(22.4, 23.0);
        result.Hourly.PrecipitationProbability.Should().ContainInOrder(10.0, 20.0);
        result.Hourly.WeatherCode.Should().ContainInOrder(0, 1);
    }

    [Fact]
    public async Task GetWeatherDataAsync_HttpError_ThrowsException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(mockHandler);
        var apiService = new ApiService(httpClient);

        // Act & Assert
        await FluentActions.Invoking(() => apiService.GetWeatherDataAsync(0, 0))
            .Should().ThrowAsync<HttpRequestException>();
    }
}
