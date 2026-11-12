using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WinuiWeatherForecastTray.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

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
                ""cloud_cover"": 15.0,
                ""pressure_msl"": 1020.1,
                ""precipitation"": 0.0,
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
        result.Current.CloudCover.Should().Be(15.0);
        result.Current.PressureMsl.Should().Be(1020.1);
        result.Current.Precipitation.Should().Be(0.0);
        result.Current.IsDay.Should().Be(1);

        result.Hourly.Should().NotBeNull();
        result.Hourly!.Time.Should().HaveCount(2);
        result.Hourly.Temperature2m.Should().ContainInOrder(22.4, 23.0);
        result.Hourly.PrecipitationProbability.Should().ContainInOrder(10.0, 20.0);
        result.Hourly.WeatherCode.Should().ContainInOrder(0, 1);
    }

    [Fact]
    public async Task GetWeatherDataAsync_PressureMslAndCurrentFields_DeserializesCorrectly()
    {
        var jsonResponse = @"{
            ""current"": {
                ""temperature_2m"": 25.5,
                ""relative_humidity_2m"": 62,
                ""apparent_temperature"": 27.5,
                ""pressure_msl"": 1020.1,
                ""cloud_cover"": 19,
                ""wind_speed_10m"": 11.8,
                ""precipitation"": 0.0,
                ""is_day"": 1,
                ""weather_code"": 0
            }
        }";

        var mockHandler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        });

        var apiService = new ApiService(new HttpClient(mockHandler));
        var result = await apiService.GetWeatherDataAsync(-12.26, -38.96);

        result.Current.Should().NotBeNull();
        result.Current!.Temperature2m.Should().Be(25.5);
        result.Current.ApparentTemperature.Should().Be(27.5);
        result.Current.RelativeHumidity2m.Should().Be(62.0);
        result.Current.PressureMsl.Should().Be(1020.1);
        result.Current.CloudCover.Should().Be(19.0);
        result.Current.WindSpeed10m.Should().Be(11.8);
        result.Current.Precipitation.Should().Be(0.0);
        result.Current.IsDay.Should().Be(1);
        result.Current.WeatherCode.Should().Be(0);
    }

    [Fact]
    public async Task GetWeatherDataAsync_HttpError_ThrowsException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(mockHandler);
        var apiService = new ApiService(httpClient, NoDelay);

        // Act & Assert
        await FluentActions.Invoking(() => apiService.GetWeatherDataAsync(0, 0))
            .Should().ThrowAsync<HttpRequestException>();
    }

    private static readonly TimeSpan[] NoDelay = { TimeSpan.Zero, TimeSpan.Zero };
    private const string ValidJson = "{\"current\":{\"temperature_2m\":21.0,\"is_day\":1}}";

    private static HttpResponseMessage Json(HttpStatusCode status) => new(status)
    {
        Content = new StringContent(ValidJson, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetWeatherDataAsync_TransientServerError_IsRetriedUntilSuccess()
    {
        int calls = 0;
        var handler = new MockHttpMessageHandler(_ => ++calls < 3 ? Json(HttpStatusCode.ServiceUnavailable) : Json(HttpStatusCode.OK));
        var apiService = new ApiService(new HttpClient(handler), NoDelay);

        var result = await apiService.GetWeatherDataAsync(0, 0);

        calls.Should().Be(3);
        result.Current!.Temperature2m.Should().Be(21.0);
    }

    [Fact]
    public async Task GetWeatherDataAsync_NetworkError_IsRetried()
    {
        int calls = 0;
        var handler = new MockHttpMessageHandler(_ => ++calls == 1
            ? throw new HttpRequestException("Simulated network failure")
            : Json(HttpStatusCode.OK));
        var apiService = new ApiService(new HttpClient(handler), NoDelay);

        var result = await apiService.GetWeatherDataAsync(0, 0);

        calls.Should().Be(2);
        result.Current!.Temperature2m.Should().Be(21.0);
    }

    [Fact]
    public async Task GetWeatherDataAsync_PersistentServerError_GivesUpAfterConfiguredRetries()
    {
        int calls = 0;
        var handler = new MockHttpMessageHandler(_ => { calls++; return Json(HttpStatusCode.InternalServerError); });
        var apiService = new ApiService(new HttpClient(handler), NoDelay);

        await FluentActions.Invoking(() => apiService.GetWeatherDataAsync(0, 0))
            .Should().ThrowAsync<HttpRequestException>();

        calls.Should().Be(3); // 1 attempt + 2 retries
    }

    [Fact]
    public async Task GetWeatherDataAsync_ClientError_IsNotRetried()
    {
        int calls = 0;
        var handler = new MockHttpMessageHandler(_ => { calls++; return Json(HttpStatusCode.BadRequest); });
        var apiService = new ApiService(new HttpClient(handler), NoDelay);

        await FluentActions.Invoking(() => apiService.GetWeatherDataAsync(0, 0))
            .Should().ThrowAsync<HttpRequestException>();

        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetWeatherDataAsync_EmptyRetryDelays_DisablesRetries()
    {
        int calls = 0;
        var handler = new MockHttpMessageHandler(_ => { calls++; return Json(HttpStatusCode.InternalServerError); });
        var apiService = new ApiService(new HttpClient(handler), Array.Empty<TimeSpan>());

        await FluentActions.Invoking(() => apiService.GetWeatherDataAsync(0, 0))
            .Should().ThrowAsync<HttpRequestException>();

        calls.Should().Be(1);
    }
}
