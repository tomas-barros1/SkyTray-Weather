using System.Threading;
using System.Threading.Tasks;

namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface IGeocodingService
{
    Task<string> GetCityNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
