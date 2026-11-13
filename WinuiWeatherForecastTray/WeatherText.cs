using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray
{
    /// <summary>Texts shared by the weather panel and the tray hover card.</summary>
    internal static class WeatherText
    {
        /// <summary>"Updated 14:05" or "Offline · updated 14:05", depending on whether the data is a cached copy.</summary>
        public static string UpdatedLabel(II18nService i18n, WeatherForecastData data)
        {
            string time = data.FetchedAtUtc.ToLocalTime().ToString("t", i18n.CurrentFormatProvider);
            string template = data.IsStale
                ? i18n.GetString("OfflineUpdatedAt", "Offline · updated {0}")
                : i18n.GetString("UpdatedAt", "Updated {0}");
            return string.Format(template, time);
        }
    }
}
