using System;
using System.IO;
using FluentAssertions;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Services;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public class ForecastCacheServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly string _filePath;

    public ForecastCacheServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"sktray_forecast_{Guid.NewGuid():N}");
        _filePath = Path.Combine(_dir, "forecast_cache.json");
    }

    private static WeatherForecastData Sample() => new()
    {
        FetchedAtUtc = new DateTime(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc),
        Current = new CurrentWeatherInfo
        {
            CityName = "Feira de Santana",
            Temperature = 22.4,
            Pressure = 1012.5,
            CustomSummaryText = "22°C"
        },
        HourlyForecast =
        {
            new HourlyForecastItem { FormattedTime = "13:00", Temperature = 23, Emoji = "☀️", RainChance = 10 }
        }
    };

    [Fact]
    public void Load_WhenNoFileExists_ReturnsNull()
    {
        new ForecastCacheService(_filePath).Load().Should().BeNull();
    }

    [Fact]
    public void Save_And_Load_RoundTripsForecast()
    {
        var service = new ForecastCacheService(_filePath);

        service.Save(Sample());
        var loaded = service.Load();

        loaded.Should().NotBeNull();
        loaded!.Current.CityName.Should().Be("Feira de Santana");
        loaded.Current.Temperature.Should().Be(22.4);
        loaded.Current.Pressure.Should().Be(1012.5);
        loaded.HourlyForecast.Should().HaveCount(1);
        loaded.HourlyForecast[0].Temperature.Should().Be(23);
        loaded.FetchedAtUtc.Should().Be(new DateTime(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc));
        loaded.IsStale.Should().BeFalse();
    }

    [Fact]
    public void Load_WhenFileIsCorrupted_ReturnsNullInsteadOfThrowing()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_filePath, "{ not valid json");

        new ForecastCacheService(_filePath).Load().Should().BeNull();
    }

    [Fact]
    public void Save_OverwritesPreviousForecast_AndLeavesNoTempFile()
    {
        var service = new ForecastCacheService(_filePath);
        service.Save(Sample());

        var newer = Sample();
        newer.Current.Temperature = 30;
        service.Save(newer);

        service.Load()!.Current.Temperature.Should().Be(30);
        File.Exists(_filePath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesFile()
    {
        var service = new ForecastCacheService(_filePath);
        service.Save(Sample());

        service.Clear();

        File.Exists(_filePath).Should().BeFalse();
        service.Load().Should().BeNull();
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }
}
