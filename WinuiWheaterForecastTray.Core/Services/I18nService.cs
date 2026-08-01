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

        string jsonFileName = ResolveLocaleFileName(culture);
        LoadTranslations(jsonFileName);
    }

    private static string ResolveLocaleFileName(string culture) => culture switch
    {
        // pt-PT intentionally maps to pt_BR.json until a dedicated pt_PT.json resource is added.
        // When pt_PT.json is created, add: string c when c.Equals("pt-PT", OrdinalIgnoreCase) => "pt_PT.json",
        string c when c.StartsWith("pt", StringComparison.OrdinalIgnoreCase) => "pt_BR.json",
        string c when c.StartsWith("es", StringComparison.OrdinalIgnoreCase) => "es_ES.json",
        string c when c.StartsWith("fr", StringComparison.OrdinalIgnoreCase) => "fr_FR.json",
        string c when c.StartsWith("de", StringComparison.OrdinalIgnoreCase) => "de_DE.json",
        _ => "en_US.json"
    };

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

        // C-07: single call — was called twice, discarding one tuple each time
        var (defaultEmoji, defaultDescription) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);
        string desc = GetString(key, defaultDescription);
        return (defaultEmoji, desc);
    }

    public string GetAirQualityDescription(double usAqi) => usAqi switch
    {
        <= 50  => GetString("Aqi_Good",    "Bom"),
        <= 100 => GetString("Aqi_Fair",    "Razoável"),
        <= 150 => GetString("Aqi_Moderate","Moderado"),
        <= 200 => GetString("Aqi_Poor",    "Ruim"),
        _      => GetString("Aqi_VeryPoor","Péssimo")
    };

    public string GetUvDescription(double uvIndex) => uvIndex switch
    {
        <= 2  => GetString("Uv_Low",     "Baixo"),
        <= 5  => GetString("Uv_Moderate","Moderado"),
        <= 7  => GetString("Uv_High",    "Alto"),
        <= 10 => GetString("Uv_VeryHigh","Muito Alto"),
        _     => GetString("Uv_Extreme", "Extremo")
    };

    public string FormatSummaryText(string cityName, string emoji, double temperature, string conditionText, double humidity, double windSpeed)
    {
        string humidityUnit = GetString("HumidityUnit", "humidity");
        string windUnit = GetString("WindUnit", "wind");

        return $"📍 {cityName}\n{emoji} {Math.Round(temperature)}°C — {conditionText}\n💧 {Math.Round(humidity)}% {humidityUnit}\n🌬️ {Math.Round(windSpeed)} km/h {windUnit}";
    }
}
