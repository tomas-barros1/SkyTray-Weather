using System.Threading;
using System.Threading.Tasks;

namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface IAirQualityService
{
    Task<double> GetUsAqiAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
