using System;

namespace WinuiWheaterForecastTray.Models;

/// <summary>
/// Represents a single hourly weather forecast slot item.
/// </summary>
public class HourlyForecastItem
{
    /// <summary>Raw ISO/API timestamp string.</summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>Formatted time string (e.g. "14:00").</summary>
    public string FormattedTime { get; set; } = string.Empty;

    /// <summary>Temperature in degrees Celsius.</summary>
    public double Temperature { get; set; }

    /// <summary>Rounded temperature string with degree symbol (e.g. "22°").</summary>
    public string DisplayTemperature => $"{Math.Round(Temperature)}°";

    /// <summary>Condition emoji icon (e.g. "🌧️").</summary>
    public string Emoji { get; set; } = string.Empty;

    /// <summary>Localized weather condition text (e.g. "Clear").</summary>
    public string ConditionText { get; set; } = string.Empty;

    /// <summary>Chance of rain percentage (0–100 %).</summary>
    public double RainChance { get; set; }

    /// <summary>Tooltip text displayed when hovering over the hourly slot card.</summary>
    public string TooltipText => $"{FormattedTime} — {ConditionText}\n{DisplayTemperature} ({Math.Round(RainChance)}%)";
}
