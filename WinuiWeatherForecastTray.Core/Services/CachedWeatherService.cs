using System;
using System.Threading;
using System.Threading.Tasks;
using WinuiWeatherForecastTray.Models;
using WinuiWeatherForecastTray.Services.Interfaces;

namespace WinuiWeatherForecastTray.Services;

/// <summary>
/// Decorator that saves every successful forecast and, when a fresh fetch fails, falls back to the
/// last saved one (flagged with <see cref="WeatherForecastData.IsStale"/>) instead of surfacing an error.
/// </summary>
public sealed class CachedWeatherService : IWeatherService
{
    /// <summary>Oldest cached forecast that is still considered useful when offline.</summary>
    public static readonly TimeSpan DefaultMaxStaleAge = TimeSpan.FromHours(6);

    private readonly IWeatherService _inner;
    private readonly IForecastCacheService _cache;
    private readonly TimeSpan _maxStaleAge;
    private readonly Func<DateTime> _utcNow;

    public CachedWeatherService(
        IWeatherService inner,
        IForecastCacheService cache,
        TimeSpan? maxStaleAge = null,
        Func<DateTime>? utcNow = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _maxStaleAge = maxStaleAge ?? DefaultMaxStaleAge;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <inheritdoc/>
    public async Task<WeatherForecastData> GetForecastAsync(double? lat = null, double? lon = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var fresh = await _inner.GetForecastAsync(lat, lon, cancellationToken).ConfigureAwait(false);
            fresh.FetchedAtUtc = _utcNow();
            fresh.IsStale = false;
            _cache.Save(fresh);
            return fresh;
        }
        // A caller-requested cancellation must propagate; an HttpClient timeout (also an
        // OperationCanceledException) is just another failure and may use the cached copy.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The cache is not keyed by location, so it is only valid for "auto-detected" requests.
            if (lat.HasValue || lon.HasValue)
                throw;

            var cached = _cache.Load();
            if (cached is null || _utcNow() - cached.FetchedAtUtc > _maxStaleAge)
                throw;

            DebugLog.Swallowed(typeof(CachedWeatherService), ex, "Fetch failed, serving cached forecast");
            cached.IsStale = true;
            return cached;
        }
    }
}
