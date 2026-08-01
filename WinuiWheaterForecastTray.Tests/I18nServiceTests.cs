using System.Threading.Tasks;
using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class I18nServiceTests
{
    // C-04: pt-PT intentionally routes to pt_BR (documented convention until pt_PT.json exists)
    [Fact]
    public void I18nService_PtPT_RoutesToPtBrBundle()
    {
        var svc = new I18nService("pt-PT");
        // pt-BR bundle has "Bom" for Aqi_Good; en-US would have "Good"
        var aqiText = svc.GetAirQualityDescription(25);
        aqiText.Should().Be("Bom", because: "pt-PT intentionally falls through to pt_BR.json until a dedicated file is added");
    }

    // C-04: en-US routes to English bundle
    [Fact]
    public void I18nService_EnUS_RoutesToEnglishBundle()
    {
        var svc = new I18nService("en-US");
        var aqiText = svc.GetAirQualityDescription(25);
        aqiText.Should().Be("Good", because: "en-US must load en_US.json");
    }

    // C-07: GetWeatherCondition must call WeatherHelper once and return correct emoji + translated desc
    [Fact]
    public void I18nService_GetWeatherCondition_ReturnsSingleCallResult()
    {
        var svc = new I18nService("en-US");

        var (emoji, desc) = svc.GetWeatherCondition(0, isDay: true);

        emoji.Should().Be("☀️", because: "weather code 0 day must be sunny emoji");
        desc.Should().Be("Sunny", because: "en-US translation for Condition_Sunny is 'Sunny'");
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
