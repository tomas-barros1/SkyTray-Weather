using System;

namespace WinuiWheaterForecastTray.Models;

public class CurrentWeatherInfo
{
    public string CityName { get; set; } = "São Paulo";
    public string DateString { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double ApparentTemperature { get; set; }
    public int WeatherCode { get; set; }
    public string ConditionText { get; set; } = "Sunny";
    public string Emoji { get; set; } = "☀️";
    public double Humidity { get; set; }
    public double WindSpeed { get; set; }
    public double CloudCover { get; set; }
    public double SurfacePressure { get; set; }
    /// <summary>Precipitation probability for the current hour (0–100 %).</summary>
    public double PrecipitationProbability { get; set; }
    public string AirQualityText { get; set; } = "Bom";
    public string UvIndexText { get; set; } = "Moderado";
    public string SunriseTime { get; set; } = "05:55";
    public string SunsetTime { get; set; } = "17:30";
    public bool IsDay { get; set; } = true;
    public string? CustomSummaryText { get; set; }

    public string SummaryText => CustomSummaryText ?? $"📍 {CityName}\n{Emoji} {Math.Round(Temperature)}°C — {ConditionText}\n💧 {Math.Round(Humidity)}% humidity\n🌬️ {Math.Round(WindSpeed)} km/h wind";
}
