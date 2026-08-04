using System;
using System.IO;
using FluentAssertions;
using WinuiWheaterForecastTray.Services;
using Xunit;

namespace WinuiWheaterForecastTray.Tests;

public class LocationCacheServiceTests : IDisposable
{
    private readonly string _tempFilePath;

    public LocationCacheServiceTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"sktray_test_cache_{Guid.NewGuid():N}.json");
    }

    [Fact]
    public void SaveLocationCache_And_GetCachedLocation_PersistsAndReadsDataCorrectly()
    {
        var service = new LocationCacheService(_tempFilePath);

        service.SaveLocationCache(-12.2664, -38.9663, "Feira de Santana");

        var cached = service.GetCachedLocation();

        cached.Should().NotBeNull();
        cached!.Latitude.Should().Be(-12.2664);
        cached.Longitude.Should().Be(-38.9663);
        cached.CityName.Should().Be("Feira de Santana");
    }

    [Fact]
    public void ClearCache_RemovesCacheFile()
    {
        var service = new LocationCacheService(_tempFilePath);

        service.SaveLocationCache(-23.5505, -46.6333, "São Paulo");
        File.Exists(_tempFilePath).Should().BeTrue();

        service.ClearCache();

        File.Exists(_tempFilePath).Should().BeFalse();
        service.GetCachedLocation().Should().BeNull();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            try { File.Delete(_tempFilePath); } catch { }
        }
    }
}
