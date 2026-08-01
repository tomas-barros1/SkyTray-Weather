using System.Threading;
using System.Threading.Tasks;

namespace WinuiWheaterForecastTray.Services.Interfaces;

public interface IGeocodingService
{
    /// <summary>
    /// Returns the city name for the given coordinates, or <c>null</c> if the reverse-geocoding
    /// call fails. Callers must handle <c>null</c> and display a suitable placeholder (e.g. "—").
    /// </summary>
    Task<string?> GetCityNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
