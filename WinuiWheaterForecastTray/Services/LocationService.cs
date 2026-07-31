using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

/// <summary>
/// Native Windows geolocation provider using <see cref="Windows.Devices.Geolocation.Geolocator"/>.
/// Handles OS privacy permissions for packaged and unpackaged WinUI 3 desktop apps.
/// </summary>
public sealed class LocationService : ILocationService
{
    /// <inheritdoc/>
    public async Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await Geolocator.RequestAccessAsync();

            if (access != GeolocationAccessStatus.Allowed)
                return null;

            var geolocator = new Geolocator
            {
                DesiredAccuracyInMeters = 500
            };

            var position = await geolocator.GetGeopositionAsync().AsTask(cancellationToken);

            return (
                position.Coordinate.Point.Position.Latitude,
                position.Coordinate.Point.Position.Longitude
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] Native OS location request failed: {ex.Message}");
            return null;
        }
    }
}
