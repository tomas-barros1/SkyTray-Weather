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
        GeolocationAccessStatus access;
        try
        {
            access = await Geolocator.RequestAccessAsync().AsTask(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] RequestAccessAsync cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] RequestAccessAsync failed: {ex.Message}");
            return null;
        }

        // C-10: distinct log per failure mode so caller can diagnose without a discriminated result
        if (access == GeolocationAccessStatus.Denied)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] Location permission denied by user or policy.");
            return null;
        }

        if (access == GeolocationAccessStatus.Unspecified)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] Location access status unspecified.");
            return null;
        }

        try
        {
            var geolocator = new Geolocator { DesiredAccuracyInMeters = 500 };

            // Hard 5-second timeout: at system startup the OS location service may not be ready yet
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(GeopositionTimeout);

            var position = await geolocator.GetGeopositionAsync().AsTask(timeoutCts.Token).ConfigureAwait(false);

            // C-03: guard against a partial Geoposition returned by the OS.
            // Coordinate and Point are reference types and can be null; Position is a struct.
            var coordinate = position?.Coordinate;
            var point = coordinate?.Point;
            if (point is null)
            {
                System.Diagnostics.Debug.WriteLine("[LocationService] Geoposition returned null coordinate chain.");
                return null;
            }

            var pos = point.Position; // BasicGeoposition is a struct — always accessible when point != null
            return (pos.Latitude, pos.Longitude);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] GetGeopositionAsync timed out — falling back to IP location.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] GetGeopositionAsync failed: {ex.Message}");
            return null;
        }
    }
}
