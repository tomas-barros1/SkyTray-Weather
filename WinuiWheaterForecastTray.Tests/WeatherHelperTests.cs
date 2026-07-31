using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class WeatherHelperTests
{
    [Theory]
    [InlineData(0, true, "☀️", "Sunny")]
    [InlineData(0, false, "🌙", "Clear")]
    [InlineData(2, true, "⛅", "Partly Cloudy")]
    [InlineData(61, true, "🌧️", "Rain")]
    [InlineData(71, true, "❄️", "Snow")]
    [InlineData(95, true, "⛈️", "Thunderstorm")]
    public void GetWeatherCondition_ReturnsExpectedEmojiAndDescription(int weatherCode, bool isDay, string expectedEmoji, string expectedDesc)
    {
        var (emoji, desc) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);

        emoji.Should().Be(expectedEmoji);
        desc.Should().Be(expectedDesc);
    }

    [Theory]
    [InlineData(0, true, "☀️")]
    [InlineData(0, false, "🌙")]
    [InlineData(1, true, "⛅")]
    [InlineData(61, true, "🌧️")]
    [InlineData(71, true, "❄️")]
    [InlineData(95, true, "⛈️")]
    public void GetTrayEmoji_ReturnsExpectedTrayEmoji(int weatherCode, bool isDay, string expectedEmoji)
    {
        var trayEmoji = WeatherHelper.GetTrayEmoji(weatherCode, isDay);

        trayEmoji.Should().Be(expectedEmoji);
    }
}
