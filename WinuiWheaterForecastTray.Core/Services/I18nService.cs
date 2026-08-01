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

    public IFormatProvider CurrentFormatProvider => CultureInfo.GetCultureInfo(
        CurrentCulture.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt-BR" : "en-US"
    );

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
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(I18nService), ex, "Failed to load locale translations, using defaults");
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
        // A-01: Single source of truth in WeatherHelper returns (Emoji, Key, Description)
        var (defaultEmoji, key, defaultDescription) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);
        string desc = GetString(key, defaultDescription);
        return (defaultEmoji, desc);
    }

    public string GetAirQualityDescription(double usAqi) => usAqi switch
    {
        <= 50 => GetString("Aqi_Good", "Bom"),
        <= 100 => GetString("Aqi_Fair", "Razoável"),
        <= 150 => GetString("Aqi_Moderate", "Moderado"),
        <= 200 => GetString("Aqi_Poor", "Ruim"),
        _ => GetString("Aqi_VeryPoor", "Péssimo")
    };

    public string GetUvDescription(double uvIndex) => uvIndex switch
    {
        <= 2 => GetString("Uv_Low", "Baixo"),
        <= 5 => GetString("Uv_Moderate", "Moderado"),
        <= 7 => GetString("Uv_High", "Alto"),
        <= 10 => GetString("Uv_VeryHigh", "Muito Alto"),
        _ => GetString("Uv_Extreme", "Extremo")
    };

    public string FormatSummaryText(string cityName, string emoji, double temperature, string conditionText, double humidity, double windSpeed)
    {
        string humidityUnit = GetString("HumidityUnit", "humidity");
        string windUnit = GetString("WindUnit", "wind");

        return $"📍 {cityName}\n{emoji} {Math.Round(temperature)}°C — {conditionText}\n💧 {Math.Round(humidity)}% {humidityUnit}\n🌬️ {Math.Round(windSpeed)} km/h {windUnit}";
    }
}
