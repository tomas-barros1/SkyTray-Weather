namespace WinuiWheaterForecastTray.Services;

public static class WeatherHelper
{
    /// <summary>
    /// Single source of truth for WMO weather code mapping to emoji, translation key, and default English description.
    /// </summary>
    public static (string Emoji, string Key, string Description) GetWeatherCondition(int weatherCode, bool isDay = true)
    {
        return weatherCode switch
        {
            0 => isDay ? ("☀️", "Condition_Sunny", "Sunny") : ("🌙", "Condition_Clear", "Clear"),
            1 => ("🌤️", "Condition_MainlyClear", "Mainly Clear"),
            2 => ("⛅", "Condition_PartlyCloudy", "Partly Cloudy"),
            3 => ("☁️", "Condition_Overcast", "Overcast"),
            45 or 48 => ("🌫️", "Condition_Foggy", "Foggy"),
            51 or 53 or 55 => ("🌧️", "Condition_Drizzle", "Drizzle"),
            56 or 57 => ("🌧️", "Condition_FreezingDrizzle", "Freezing Drizzle"),
            61 or 63 or 65 => ("🌧️", "Condition_Rain", "Rain"),
            66 or 67 => ("🌧️", "Condition_FreezingRain", "Freezing Rain"),
            71 or 73 or 75 or 77 => ("❄️", "Condition_Snow", "Snow"),
            80 or 81 or 82 => ("🌧️", "Condition_RainShowers", "Rain Showers"),
            85 or 86 => ("❄️", "Condition_SnowShowers", "Snow Showers"),
            95 or 96 or 99 => ("⛈️", "Condition_Thunderstorm", "Thunderstorm"),
            _ => isDay ? ("☀️", "Condition_Sunny", "Sunny") : ("🌙", "Condition_Clear", "Clear")
        };
    }

    public static string GetTrayEmoji(int weatherCode, bool isDay = true)
    {
        return weatherCode switch
        {
            0 => isDay ? "☀️" : "🌙",
            1 or 2 or 3 => "⛅",
            71 or 73 or 75 or 77 or 85 or 86 => "❄️",
            95 or 96 or 99 => "⛈️",
            _ => "🌧️"
        };
    }
}
