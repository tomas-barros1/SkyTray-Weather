using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class GeocodingServiceTests
{
    // C-09: network failure must return null, not "São Paulo"
    [Fact]
    public async Task GetCityNameAsync_NetworkFailure_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(_ => throw new HttpRequestException("Simulated failure"));
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var service = new GeocodingService(client);

        var result = await service.GetCityNameAsync(-12.2664, -38.9663);

        result.Should().BeNull(because: "on failure the service must return null, not a hardcoded city name");
    }

    // C-09: valid response returns the city name
    [Fact]
    public async Task GetCityNameAsync_ValidResponse_ReturnsCityName()
    {
        const string json = """{"city":"Feira de Santana","locality":"Feira de Santana","principalSubdivision":"Bahia","countryName":"Brazil"}""";
        var client = new HttpClient(new MockHttpMessageHandler(json)) { Timeout = TimeSpan.FromSeconds(5) };
        var service = new GeocodingService(client);

        var result = await service.GetCityNameAsync(-12.2664, -38.9663);

        result.Should().Be("Feira de Santana");
    }

    // C-09: HTTP error status returns null
    [Fact]
    public async Task GetCityNameAsync_HttpErrorStatus_ReturnsNull()
    {
        var client = new HttpClient(new MockHttpMessageHandler("{}", HttpStatusCode.InternalServerError))
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        var service = new GeocodingService(client);

        var result = await service.GetCityNameAsync(-12.2664, -38.9663);

        result.Should().BeNull(because: "HTTP 500 response must yield null, not a hardcoded fallback");
    }
}
