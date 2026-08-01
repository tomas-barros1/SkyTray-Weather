using System;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public class AutostartService : IAutostartService
{
    private const string AppName = "SkyTrayWeather";
    private const string LegacyAppName = "WinuiWheaterForecastTray";
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsAutostartEnabled()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                if (key != null)
                {
                    return key.GetValue(AppName) != null || key.GetValue(LegacyAppName) != null;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(AutostartService), ex, "IsAutostartEnabled check failed");
        }
        return false;
    }

    public void SetAutostart(bool enable)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key != null)
                {
                    if (enable)
                    {
                        string? exePath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            if (exePath.Contains('"'))
                                throw new ArgumentException("Process path must not contain double-quote characters.", nameof(exePath));

                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                        key.DeleteValue(LegacyAppName, false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Swallowed(typeof(AutostartService), ex, "SetAutostart write failed");
        }
    }
}
