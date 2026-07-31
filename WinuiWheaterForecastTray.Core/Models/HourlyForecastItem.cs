using System;

namespace WinuiWheaterForecastTray.Models;

public class HourlyForecastItem
{
    public string Time { get; set; } = string.Empty;
    public string FormattedTime { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string DisplayTemperature => $"{Math.Round(Temperature)}°";
    public string Emoji { get; set; } = "☀️";
    public double RainChance { get; set; }
}
