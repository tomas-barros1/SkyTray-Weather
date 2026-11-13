namespace WinuiWeatherForecastTray.Services.Interfaces;

public interface ISettingsService
{
    int GetRefreshIntervalMinutes();
    void SetRefreshIntervalMinutes(int minutes);

    /// <summary>Whether hovering the tray icon shows the weather card instead of the classic tooltip (default: true).</summary>
    bool GetHoverCardEnabled();

    void SetHoverCardEnabled(bool enabled);
}
