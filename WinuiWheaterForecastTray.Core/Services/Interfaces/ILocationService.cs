using System.Threading;
using System.Threading.Tasks;

namespace WinuiWheaterForecastTray.Services.Interfaces;

/// <summary>
/// Service contract for resolving user geographic coordinates.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Asynchronously requests current geographic coordinates (Latitude, Longitude).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple containing (Latitude, Longitude) if successful;
    /// otherwise <c>null</c> if permission is denied, disabled, or coordinates cannot be resolved.
    /// Callers should gracefully fall back to alternative location providers when <c>null</c> is returned.
    /// </returns>
    Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default);
}
