using FluentAssertions;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

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
    // Sun / Clear Night
    [InlineData(0, true, "☀️")]
    [InlineData(0, false, "🌙")]
    // Cloudy / Overcast
    [InlineData(1, true, "⛅")]
    [InlineData(2, true, "⛅")]
    [InlineData(3, true, "⛅")]
    // Rain (Drizzle 51/53/55, Freezing Drizzle 56/57, Rain 61/63/65, Freezing Rain 66/67, Rain Showers 80/81/82)
    [InlineData(51, true, "🌧️")]
    [InlineData(53, true, "🌧️")]
    [InlineData(61, true, "🌧️")]
    [InlineData(63, true, "🌧️")]
    [InlineData(65, true, "🌧️")]
    [InlineData(80, true, "🌧️")]
    [InlineData(81, true, "🌧️")]
    // Snow (71/73/75/77/85/86)
    [InlineData(71, true, "❄️")]
    [InlineData(73, true, "❄️")]
    [InlineData(85, true, "❄️")]
    // Thunderstorm (95/96/99)
    [InlineData(95, true, "⛈️")]
    [InlineData(96, true, "⛈️")]
    [InlineData(99, true, "⛈️")]
    public void GetTrayEmoji_ReturnsExpectedTrayEmoji(int weatherCode, bool isDay, string expectedEmoji)
    {
        var trayEmoji = WeatherHelper.GetTrayEmoji(weatherCode, isDay);

        trayEmoji.Should().Be(expectedEmoji);
    }

    [Theory]
    [InlineData(0, true, WeatherIcon.ClearDay)]
    [InlineData(0, false, WeatherIcon.ClearNight)]
    [InlineData(1, true, WeatherIcon.PartlyCloudyDay)]
    [InlineData(2, false, WeatherIcon.PartlyCloudyNight)]
    [InlineData(3, true, WeatherIcon.Cloudy)]      // overcast is no longer shown as "partly cloudy"
    [InlineData(3, false, WeatherIcon.Cloudy)]
    [InlineData(45, true, WeatherIcon.Fog)]        // fog is no longer shown as "rain"
    [InlineData(48, false, WeatherIcon.Fog)]
    [InlineData(51, true, WeatherIcon.Rain)]
    [InlineData(57, true, WeatherIcon.Rain)]
    [InlineData(61, true, WeatherIcon.Rain)]
    [InlineData(67, false, WeatherIcon.Rain)]
    [InlineData(80, true, WeatherIcon.Rain)]
    [InlineData(82, false, WeatherIcon.Rain)]
    [InlineData(71, true, WeatherIcon.Snow)]
    [InlineData(77, false, WeatherIcon.Snow)]
    [InlineData(85, true, WeatherIcon.Snow)]
    [InlineData(86, false, WeatherIcon.Snow)]
    [InlineData(95, true, WeatherIcon.Thunderstorm)]
    [InlineData(99, false, WeatherIcon.Thunderstorm)]
    [InlineData(1234, true, WeatherIcon.ClearDay)]    // unknown code: same default as GetWeatherCondition
    [InlineData(1234, false, WeatherIcon.ClearNight)]
    public void GetIcon_MapsWmoCodeToIcon(int weatherCode, bool isDay, WeatherIcon expected)
    {
        WeatherHelper.GetIcon(weatherCode, isDay).Should().Be(expected);
    }

    [Fact]
    public void ToFileName_IsUniqueAndNonEmptyForEveryRealIcon()
    {
        var names = new List<string>();
        foreach (WeatherIcon icon in Enum.GetValues<WeatherIcon>())
        {
            if (icon == WeatherIcon.Unknown)
            {
                icon.ToFileName().Should().BeEmpty();
                continue;
            }

            names.Add(icon.ToFileName());
        }

        names.Should().OnlyContain(n => n.Length > 0);
        names.Should().OnlyHaveUniqueItems();
    }
}
