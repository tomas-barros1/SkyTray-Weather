using System.Threading.Tasks;
using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class ApiContractIntegrationTests
{
    [Theory]
    [InlineData(-23.5505, -46.6333, "São Paulo")]
    [InlineData(-12.2664, -38.9663, "Feira de Santana")]
    public async Task LiveApiEndpoint_ReturnsValidContractData(double lat, double lon, string cityLabel)
    {
        // Arrange — use a generous 30s timeout for integration tests (production uses 5s)
        var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var apiService = new ApiService(httpClient);

        // Act
        var result = await apiService.GetWeatherDataAsync(lat, lon);

        // Assert
        result.Should().NotBeNull(because: $"API must return data for {cityLabel}");
        result.Current.Should().NotBeNull(because: $"current block must exist for {cityLabel}");
        result.Current!.Time.Should().NotBeNullOrEmpty();
        result.Hourly.Should().NotBeNull(because: $"hourly block must exist for {cityLabel}");
        result.Hourly!.Time.Should().NotBeEmpty();
        result.Hourly.Temperature2m.Should().NotBeEmpty();
        result.Hourly.PrecipitationProbability.Should().NotBeEmpty(because: "precipitation_probability is required for rain display");
        result.Hourly.WeatherCode.Should().NotBeEmpty();

        // Precipitation probability values must be 0–100
        foreach (var prob in result.Hourly.PrecipitationProbability)
        {
            prob.Should().BeInRange(0, 100, because: $"precipitation_probability must be a percentage (0-100) for {cityLabel}");
        }

        // precipitation in current block is mm (not mm/h) — we no longer display this directly
        result.Current.Precipitation.Should().BeGreaterThanOrEqualTo(0);
    }
}
