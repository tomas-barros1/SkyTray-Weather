using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Services;
using WinuiWeatherForecastTray.Services.Interfaces;
using Xunit;

namespace WinuiWeatherForecastTray.Tests;

public class CachedWeatherServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 11, 18, 0, 0, DateTimeKind.Utc);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"sktray_cached_{Guid.NewGuid():N}");
    private readonly ForecastCacheService _cache;

    public CachedWeatherServiceTests()
    {
        _cache = new ForecastCacheService(Path.Combine(_dir, "forecast_cache.json"));
    }

    private static WeatherForecastData Forecast(double temperature) => new()
    {
        Current = new CurrentWeatherInfo { CityName = "Feira de Santana", Temperature = temperature }
    };

    private static Mock<IWeatherService> InnerReturning(WeatherForecastData data)
    {
        var inner = new Mock<IWeatherService>();
        inner.Setup(w => w.GetForecastAsync(It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
        return inner;
    }

    private static Mock<IWeatherService> InnerThrowing(Exception ex)
    {
        var inner = new Mock<IWeatherService>();
        inner.Setup(w => w.GetForecastAsync(It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return inner;
    }

    [Fact]
    public async Task GetForecastAsync_OnSuccess_ReturnsFreshDataAndPersistsIt()
    {
        var service = new CachedWeatherService(InnerReturning(Forecast(25)).Object, _cache, utcNow: () => Now);

        var result = await service.GetForecastAsync();

        result.IsStale.Should().BeFalse();
        result.FetchedAtUtc.Should().Be(Now);
        _cache.Load()!.Current.Temperature.Should().Be(25);
    }

    [Fact]
    public async Task GetForecastAsync_WhenFetchFails_ServesCachedForecastMarkedStale()
    {
        await new CachedWeatherService(InnerReturning(Forecast(25)).Object, _cache, utcNow: () => Now).GetForecastAsync();
        var offline = new CachedWeatherService(
            InnerThrowing(new HttpRequestException("offline")).Object, _cache, utcNow: () => Now.AddMinutes(30));

        var result = await offline.GetForecastAsync();

        result.IsStale.Should().BeTrue();
        result.Current.Temperature.Should().Be(25);
        result.FetchedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task GetForecastAsync_WhenFetchTimesOut_ServesCachedForecast()
    {
        // HttpClient reports its own timeout as TaskCanceledException, which is not a user cancellation.
        await new CachedWeatherService(InnerReturning(Forecast(25)).Object, _cache, utcNow: () => Now).GetForecastAsync();
        var timingOut = new CachedWeatherService(
            InnerThrowing(new TaskCanceledException("timeout")).Object, _cache, utcNow: () => Now.AddMinutes(5));

        var result = await timingOut.GetForecastAsync();

        result.IsStale.Should().BeTrue();
    }

    [Fact]
    public async Task GetForecastAsync_WhenFetchFailsAndNothingCached_Rethrows()
    {
        var service = new CachedWeatherService(
            InnerThrowing(new HttpRequestException("offline")).Object, _cache, utcNow: () => Now);

        await FluentActions.Invoking(() => service.GetForecastAsync())
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetForecastAsync_WhenCachedForecastIsTooOld_Rethrows()
    {
        await new CachedWeatherService(InnerReturning(Forecast(25)).Object, _cache, utcNow: () => Now).GetForecastAsync();
        var service = new CachedWeatherService(
            InnerThrowing(new HttpRequestException("offline")).Object, _cache,
            maxStaleAge: TimeSpan.FromHours(6), utcNow: () => Now.AddHours(7));

        await FluentActions.Invoking(() => service.GetForecastAsync())
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetForecastAsync_WithExplicitCoordinates_NeverUsesCachedForecast()
    {
        // The cache is not keyed by location, so it must not answer for a different place.
        await new CachedWeatherService(InnerReturning(Forecast(25)).Object, _cache, utcNow: () => Now).GetForecastAsync();
        var service = new CachedWeatherService(
            InnerThrowing(new HttpRequestException("offline")).Object, _cache, utcNow: () => Now.AddMinutes(5));

        await FluentActions.Invoking(() => service.GetForecastAsync(10.0, 20.0))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetForecastAsync_WhenCallerCancels_PropagatesCancellationInsteadOfServingCache()
    {
        await new CachedWeatherService(InnerReturning(Forecast(25)).Object, _cache, utcNow: () => Now).GetForecastAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var service = new CachedWeatherService(
            InnerThrowing(new OperationCanceledException(cts.Token)).Object, _cache, utcNow: () => Now.AddMinutes(5));

        await FluentActions.Invoking(() => service.GetForecastAsync(null, null, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }
}
