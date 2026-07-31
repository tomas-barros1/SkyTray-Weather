namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface IAutostartService
{
    bool IsAutostartEnabled();
    void SetAutostart(bool enable);
}
