using System.Threading.Tasks;
using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class ApiContractIntegrationTests
{
    [Fact]
    public async Task LiveApiEndpoint_ReturnsValidContractData()
    {
        // Arrange
        var apiService = new ApiService();
        double lat = -23.5505; // São Paulo
        double lon = -46.6333;

        // Act
        var result = await apiService.GetWeatherDataAsync(lat, lon);

        // Assert
        result.Should().NotBeNull();
        result.Current.Should().NotBeNull();
        result.Current!.Time.Should().NotBeNullOrEmpty();
        result.Hourly.Should().NotBeNull();
        result.Hourly!.Time.Should().NotBeEmpty();
        result.Hourly.Temperature2m.Should().NotBeEmpty();
        result.Hourly.PrecipitationProbability.Should().NotBeEmpty();
        result.Hourly.WeatherCode.Should().NotBeEmpty();
    }
}
