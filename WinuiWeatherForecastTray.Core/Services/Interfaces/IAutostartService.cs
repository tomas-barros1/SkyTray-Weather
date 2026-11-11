namespace WinuiWeatherForecastTray.Services.Interfaces;

public interface IAutostartService
{
    bool IsAutostartEnabled();
    void SetAutostart(bool enable);
}
