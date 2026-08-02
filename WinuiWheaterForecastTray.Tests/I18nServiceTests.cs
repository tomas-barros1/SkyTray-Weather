using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class I18nServiceTests
{
    [Theory]
    [InlineData("pt-BR", "Bom", "Ensolarado")]
    [InlineData("pt-PT", "Bom", "Ensolarado")]
    [InlineData("en-US", "Good", "Sunny")]
    [InlineData("es-ES", "Bueno", "Soleado")]
    [InlineData("fr-FR", "Bon", "Ensoleillé")]
    [InlineData("de-DE", "Gut", "Sonnig")]
    public void I18nService_SupportedLocales_LoadCorrectBundles(string culture, string expectedAqi, string expectedSunny)
    {
        var svc = new I18nService(culture);

        var aqiText = svc.GetAirQualityDescription(25);
        var (_, condition) = svc.GetWeatherCondition(0, isDay: true);

        aqiText.Should().Be(expectedAqi, because: $"{culture} bundle must translate Aqi_Good correctly");
        condition.Should().Be(expectedSunny, because: $"{culture} bundle must translate Condition_Sunny correctly");
    }

    [Fact]
    public void I18nService_GetWeatherCondition_NightClear_ReturnsMoonEmoji()
    {
        var svc = new I18nService("en-US");

        var (emoji, desc) = svc.GetWeatherCondition(0, isDay: false);

        emoji.Should().Be("🌙");
        desc.Should().Be("Clear");
    }
}
