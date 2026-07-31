using System.Threading;
using System.Threading.Tasks;

namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface ILocationService
{
    Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default);
}
