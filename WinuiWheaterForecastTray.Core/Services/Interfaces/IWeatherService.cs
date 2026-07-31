using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.Models;

namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface IWeatherService
{
    Task<WeatherForecastData> GetForecastAsync(double? lat = null, double? lon = null, CancellationToken cancellationToken = default);
}
