using System;

namespace WinuiWheaterForecastTray.Services;

public class AutostartService : Interfaces.IAutostartService
{
    private const string AppName = "WinuiWheaterForecastTray";
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsAutostartEnabled()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
        }
        catch
        {
            // Silence permission/registry exceptions
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
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
        }
        catch
        {
            // Silence registry write exceptions
        }
    }
}
