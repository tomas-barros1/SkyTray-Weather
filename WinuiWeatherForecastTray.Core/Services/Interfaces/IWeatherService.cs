using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Models;

namespace WinuiWeatherForecastTray.Services.Interfaces;

public interface IWeatherService
{
    Task<WeatherForecastData> GetForecastAsync(double? lat = null, double? lon = null, CancellationToken cancellationToken = default);
}
