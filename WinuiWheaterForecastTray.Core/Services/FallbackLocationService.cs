using System.Threading;
using System.Threading.Tasks;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public class FallbackLocationService : ILocationService
{
    public Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        (double Latitude, double Longitude)? location = (-23.5505, -46.6333);
        return Task.FromResult(location);
    }
}
