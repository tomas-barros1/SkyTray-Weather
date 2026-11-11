using System.Threading;
using System.Threading.Tasks;

namespace WinuiWeatherForecastTray.Services.Interfaces;

public interface IAirQualityService
{
    Task<double> GetUsAqiAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
