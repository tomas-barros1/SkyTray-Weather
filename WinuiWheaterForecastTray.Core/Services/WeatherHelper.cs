namespace WinuiWheaterForecastTray.Services;

public static class WeatherHelper
{
    public static (string Emoji, string Description) GetWeatherCondition(int weatherCode, bool isDay = true)
    {
        return weatherCode switch
        {
            0 => isDay ? ("☀️", "Sunny") : ("🌙", "Clear"),
            1 => isDay ? ("🌤️", "Mainly Clear") : ("🌙", "Mainly Clear"),
            2 => isDay ? ("⛅", "Partly Cloudy") : ("☁️", "Partly Cloudy"),
            3 => ("☁️", "Overcast"),
            45 or 48 => ("🌫️", "Foggy"),
            51 or 53 or 55 => ("🌧️", "Drizzle"),
            56 or 57 => ("🌧️", "Freezing Drizzle"),
            61 or 63 or 65 => ("🌧️", "Rain"),
            66 or 67 => ("🌧️", "Freezing Rain"),
            71 or 73 or 75 or 77 => ("❄️", "Snow"),
            80 or 81 or 82 => ("🌧️", "Rain Showers"),
            85 or 86 => ("❄️", "Snow Showers"),
            95 or 96 or 99 => ("⛈️", "Thunderstorm"),
            _ => isDay ? ("☀️", "Clear") : ("🌙", "Clear")
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
