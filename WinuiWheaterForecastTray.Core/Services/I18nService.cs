using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class I18nService : II18nService
{
    private readonly Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentCulture { get; }

    public I18nService(string? localeOverride = null)
    {
        var culture = localeOverride ?? CultureInfo.CurrentUICulture.Name;
        CurrentCulture = culture;

        bool isPortuguese = culture.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
        string jsonFileName = isPortuguese ? "pt_BR.json" : "en_US.json";

        LoadTranslations(jsonFileName);
    }

    private void LoadTranslations(string jsonFileName)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "Resources", jsonFileName);
            if (!File.Exists(path))
            {
                path = Path.Combine(baseDir, jsonFileName);
            }

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        _translations[kv.Key] = kv.Value;
                    }
                }
            }
        }
        catch
        {
            // Graceful fallback to default values
        }
    }

    public string GetString(string key, string fallback = "")
    {
        if (_translations.TryGetValue(key, out var val))
        {
            return val;
        }
        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    public (string Emoji, string Description) GetWeatherCondition(int weatherCode, bool isDay = true)
    {
        string key = weatherCode switch
        {
            0 => isDay ? "Condition_Sunny" : "Condition_Clear",
            1 => "Condition_MainlyClear",
            2 => "Condition_PartlyCloudy",
            3 => "Condition_Overcast",
            45 or 48 => "Condition_Foggy",
            51 or 53 or 55 => "Condition_Drizzle",
            56 or 57 => "Condition_FreezingDrizzle",
            61 or 63 or 65 => "Condition_Rain",
            66 or 67 => "Condition_FreezingRain",
            71 or 73 or 75 or 77 => "Condition_Snow",
            80 or 81 or 82 => "Condition_RainShowers",
            85 or 86 => "Condition_SnowShowers",
            95 or 96 or 99 => "Condition_Thunderstorm",
            _ => isDay ? "Condition_Sunny" : "Condition_Clear"
        };

        var (emoji, _) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);
        string desc = GetString(key, WeatherHelper.GetWeatherCondition(weatherCode, isDay).Description);

        return (emoji, desc);
    }

    public string GetAirQualityDescription(double usAqi)
    {
        if (usAqi <= 50) return GetString("Aqi_Good", "Bom");
        if (usAqi <= 100) return GetString("Aqi_Fair", "Razoável");
        if (usAqi <= 150) return GetString("Aqi_Moderate", "Moderado");
        if (usAqi <= 200) return GetString("Aqi_Poor", "Ruim");
        return GetString("Aqi_VeryPoor", "Péssimo");
    }

    public string GetUvDescription(double uvIndex)
    {
        if (uvIndex <= 2) return GetString("Uv_Low", "Baixo");
        if (uvIndex <= 5) return GetString("Uv_Moderate", "Moderado");
        if (uvIndex <= 7) return GetString("Uv_High", "Alto");
        if (uvIndex <= 10) return GetString("Uv_VeryHigh", "Muito Alto");
        return GetString("Uv_Extreme", "Extremo");
    }

    public string FormatSummaryText(string cityName, string emoji, double temperature, string conditionText, double humidity, double windSpeed)
    {
        string humidityUnit = GetString("HumidityUnit", "humidity");
        string windUnit = GetString("WindUnit", "wind");

        return $"📍 {cityName}\n{emoji} {Math.Round(temperature)}°C — {conditionText}\n💧 {Math.Round(humidity)}% {humidityUnit}\n🌬️ {Math.Round(windSpeed)} km/h {windUnit}";
    }
}
