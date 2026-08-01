using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class WeatherHelperTests
{
    [Theory]
    [InlineData(0, true, "☀️", "Condition_Sunny", "Sunny")]
    [InlineData(0, false, "🌙", "Condition_Clear", "Clear")]
    [InlineData(2, true, "⛅", "Condition_PartlyCloudy", "Partly Cloudy")]
    [InlineData(61, true, "🌧️", "Condition_Rain", "Rain")]
    [InlineData(71, true, "❄️", "Condition_Snow", "Snow")]
    [InlineData(95, true, "⛈️", "Condition_Thunderstorm", "Thunderstorm")]
    public void GetWeatherCondition_ReturnsExpectedEmojiKeyAndDescription(int weatherCode, bool isDay, string expectedEmoji, string expectedKey, string expectedDesc)
    {
        var (emoji, key, desc) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);

        emoji.Should().Be(expectedEmoji);
        key.Should().Be(expectedKey);
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
