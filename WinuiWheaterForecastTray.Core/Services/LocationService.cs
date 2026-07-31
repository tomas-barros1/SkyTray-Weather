using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Native Windows geolocation provider using <see cref="Windows.Devices.Geolocation.Geolocator"/>.
/// Handles OS privacy permissions for packaged and unpackaged WinUI 3 desktop apps.
/// Uses a short timeout so it doesn't block startup while the OS location service is initializing.
/// </summary>
public sealed class LocationService : ILocationService
{
    private static readonly TimeSpan GeopositionTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc/>
    public async Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await Geolocator.RequestAccessAsync().AsTask(cancellationToken).ConfigureAwait(false);

            if (access != GeolocationAccessStatus.Allowed)
                return null;

            var geolocator = new Geolocator
            {
                DesiredAccuracyInMeters = 500
            };

            // Use a combined CancellationToken with a hard 5-second timeout.
            // At system startup the OS location service may not be ready yet,
            // and GetGeopositionAsync can stall indefinitely without a timeout.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(GeopositionTimeout);

            var position = await geolocator.GetGeopositionAsync().AsTask(timeoutCts.Token).ConfigureAwait(false);

            return (
                position.Coordinate.Point.Position.Latitude,
                position.Coordinate.Point.Position.Longitude
            );
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] GetGeopositionAsync timed out — falling back to IP location.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] Native OS location request failed: {ex.Message}");
            return null;
        }
    }
}
