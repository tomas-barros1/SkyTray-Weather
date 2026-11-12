namespace WinuiWeatherForecastTray.Services.Interfaces;

public interface IAutostartService
{
    bool IsAutostartEnabled();
    void SetAutostart(bool enable);

    /// <summary>
    /// If autostart is enabled, re-writes the Run entry so it points at the currently running executable
    /// (and removes the legacy entry). Keeps autostart working after the install path or exe name changes.
    /// </summary>
    void RefreshAutostartPath();
}
