namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface II18nService
{
    string CurrentCulture { get; }
    string GetString(string key, string fallback = "");
    (string Emoji, string Description) GetWeatherCondition(int weatherCode, bool isDay = true);
    string GetAirQualityDescription(double usAqi);
    string GetUvDescription(double uvIndex);
    string FormatSummaryText(string cityName, string emoji, double temperature, string conditionText, double humidity, double windSpeed);
}
