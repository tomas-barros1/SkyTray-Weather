namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface ISettingsService
{
    int GetRefreshIntervalMinutes();
    void SetRefreshIntervalMinutes(int minutes);
}
