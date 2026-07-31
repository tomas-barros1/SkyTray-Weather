using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using WinuiWheaterForecastTray.Services.Interfaces;

namespace WinuiWheaterForecastTray.Services;

public sealed class LocationService : ILocationService
{
    public async Task<(double Latitude, double Longitude)?> GetLocationAsync(CancellationToken cancellationToken = default)
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
}
