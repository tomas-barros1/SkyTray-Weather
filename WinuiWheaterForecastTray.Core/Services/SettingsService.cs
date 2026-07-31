using System;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsRegistryKey = @"Software\WinuiWheaterForecastTray";
    private const string RefreshIntervalKey = "RefreshIntervalMinutes";
    private const int DefaultInterval = 15;

    public int GetRefreshIntervalMinutes()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SettingsRegistryKey, false);
                var val = key?.GetValue(RefreshIntervalKey);
                if (val is int minutes && minutes > 0)
                {
                    return minutes;
                }
            }
        }
        catch
        {
            // Silence registry exceptions
        }
        return DefaultInterval;
    }

    public void SetRefreshIntervalMinutes(int minutes)
    {
        try
        {
            if (OperatingSystem.IsWindows() && minutes > 0)
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(SettingsRegistryKey, true);
                key?.SetValue(RefreshIntervalKey, minutes);
            }
        }
        catch
        {
            // Silence registry write exceptions
        }
    }
}
