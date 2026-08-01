# 03 — Architecture Fixes

Structural remedies for the design smells that survived because the per-file fixes in waves 01/02 only patch individual symptoms.
**Land these after `02-performance-fixes.md`.**

> Architectural drift accumulated because each service was written independently with similar shapes. The remedies below are **scope-limited** — each one reduces concept count or makes boundaries explicit, none introduces a DI container, none requires rewriting working tests.

---

## A-01 [Required] Unify the WMO weather-code mapping

**Files:**
- `WinuiWheaterForecastTray.Core/Services/WeatherHelper.cs` (the mapping)
- `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~80-100` (the duplicate)

**Severity:** Required (two sources of truth)

The mapping from WMO weather code to `(emoji, description)` exists in both `WeatherHelper.GetWeatherCondition` and `I18nService.GetWeatherCondition`. The latter calls the former twice. Add a new weather code and you must update both files — easy to forget.

**Remedy:** Make `WeatherHelper.GetWeatherCondition` the single source of truth, returning a stable `WeatherConditionKey` enum (or just a `(Emoji, string Key)` tuple). `I18nService` translates the key, never re-derives emoji or description.

```csharp
// WeatherHelper.cs
public enum WeatherConditionKey { Sunny, Clear, PartlyCloudy, Overcast, Foggy, Drizzle, Rain, Snow, Thunderstorm }
public static (string Emoji, WeatherConditionKey Key, string DefaultDescription) GetWeatherCondition(int code, bool isDay);
```

```csharp
// I18nService.cs
var (emoji, key, fallback) = WeatherHelper.GetWeatherCondition(code, isDay);
var description = GetString($"weather.{key}", fallback);
return (emoji, description);
```

Update tests in `WeatherHelperTests.cs` accordingly. New tests should assert `I18nService.GetWeatherCondition` returns the same emoji as `WeatherHelper` for every supported code.

---

## A-02 [Required] `ILocationService` overloaded with two unrelated implementations

**Files:**
- `WinuiWheaterForecastTray.Core/Services/LocationService.cs` (Windows `Geolocator`)
- `WinuiWheaterForecastTray.Core/Services/IpLocationService.cs` (IP geolocation)
- `WinuiWheaterForecastTray.Core/Services/Interfaces/ILocationService.cs`

**Severity:** Required (interface overloads two strategies)

`ILocationService` is implemented by both the native-Windows-geolocator service and the IP-fallback service. Their implementations have nothing in common — one calls `Windows.Devices.Geolocation.Geolocator`, the other calls `ipapi.co`. Both just happen to return `(lat, lon)?`.

**Remedy:** Rename the interface to `ICoordinateProvider` (or split into `INativeLocationService` + `IIpLocationService` with their own distinct interfaces), and document the strategy chain in `WeatherService`:

```csharp
// Option A: rename + chain
public interface ICoordinateProvider { Task<(double, double)?> GetCoordinatesAsync(...); }
public sealed class NativeLocationProvider : ICoordinateProvider { ... }
public sealed class IpLocationProvider : ICoordinateProvider { ... }

// In WeatherService
public WeatherService(..., IEnumerable<ICoordinateProvider> providers)
{
    foreach (var p in providers) _providers.Add(p);
}

private async Task<(double, double)?> GetCoordinatesAsync(...)
{
    foreach (var p in _providers)
    {
        try { if (await p.GetCoordinatesAsync(ct) is var coords) return coords; }
        catch { /* try next */ }
    }
    return null;
}
```

Prefer Option A — it makes the fallback chain explicit, eliminates the manual try/catch in `WeatherService.GetForecastAsync`, and gives future strategies (e.g., user-typed lat/lon) a clear extension point.

The constructor injection for `ILocationService? ipLocationService = null` becomes `IEnumerable<ICoordinateProvider> providers = null`.

Update `WeatherServiceTests.cs` to reflect the new ctor.

---

## A-03 [Required] `WeatherService.GetForecastAsync` is too long

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs`
**Severity:** Required (file size)

`GetForecastAsync` is ~190 lines and mixes:
1. Coordinate resolution
2. Three API calls (sequential — see P-01)
3. Building `CurrentWeatherInfo`
4. Building the 6-hour forecast list
5. Hourly time-window matching
6. i18n lookup
7. Date/culture formatting

**Remedy:** Split into focused helpers. **Push for the version where whole branches disappear, not just relocate.** A reasonable shape:

```csharp
public async Task<WeatherForecastData> GetForecastAsync(...)
{
    var coords = await GetCoordinatesAsync(...);
    var (dto, aqi, city) = await FetchAllAsync(coords, ct);
    return BuildForecast(dto, aqi, city, ct);
}

private async Task<(ApiResponseDTO dto, double? aqi, string city)> FetchAllAsync(...);
private WeatherForecastData BuildForecast(ApiResponseDTO dto, double? aqi, string city, ...);
private CurrentWeatherInfo BuildCurrent(ApiResponseDTO dto, double? aqi, string city, ...);
private List<HourlyForecastItem> BuildHourly(ApiResponseDTO.Hourly hourly, ...);
```

`BuildHourly` then contains the single-pass loop from P-03.

---

## A-04 [Required] Replace `empty catch {}` with a project-wide logging policy

**Files:** every service that swallows exceptions
- `AirQualityService.cs`
- `GeocodingService.cs`
- `IpLocationService.cs`
- `ApiService.cs` (no catch — but exception semantics are inconsistent; see C-08)
- `AutostartService.cs`
- `SettingsService.cs`

**Severity:** Required (observability + maintainability)

Every service swallows exceptions with `catch { }` (some with `catch { }` empty, some with `Debug.WriteLine`). No `ILogger` is configured. Production users have no way to know why the AQI shows `42.0` instead of a real number.

**Remedy (minimal, no DI):** Pick one project-wide pattern. Two acceptable options:

**Option A — introduce `ILogger<T>` via `Microsoft.Extensions.Logging.Abstractions`** (no DI container needed; pass the logger in by hand from `MainWindow`).

```csharp
// In WinuiWheaterForecastTray.Core/Services/AirQualityService.cs
private readonly ILogger<AirQualityService>? _logger;
public AirQualityService(HttpClient? httpClient = null, ILogger<AirQualityService>? logger = null)
{
    _httpClient = httpClient ?? DefaultHttpClient;
    _logger = logger;
}

public async Task<double> GetUsAqiAsync(...)
{
    try { ... }
    catch (Exception ex) { _logger?.LogWarning(ex, "AQI fetch failed for ({Lat},{Lon})", lat, lon); return DefaultAqi; }
}
```

**Option B — keep the `Debug.WriteLine` pattern (consistent with `LocationService`) but make it a shared helper.**

```csharp
internal static class DebugLog
{
    public static void Swallowed(Type owner, Exception ex)
        => System.Diagnostics.Debug.WriteLine($"[{owner.Name}] {ex.GetType().Name}: {ex.Message}");
}
```

Prefer Option A — `ILogger` is the .NET standard and makes future DI migration easier.

Either way, **every `catch {}` site must log.** Add a `// TODO:` comment at each swallow site explaining what failure looks like to the user, until the policy is enforced.

---

## A-05 [Required] `WeatherService` mixes `string` culture names with `CultureInfo`

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs:~95, ~141`
**Severity:** Required (consistency; also see P-04)

Hardcoded `new CultureInfo("pt-BR")` and `new CultureInfo("en-US")` references are interleaved with calls to `I18nService.CurrentCulture`. The `I18nService` already knows the culture — use it.

**Remedy:** Inject `IFormatProvider` (or expose `I18nService.CurrentFormatProvider`) so date/number formatting goes through the i18n layer.

```csharp
// In I18nService
public IFormatProvider CurrentFormatProvider => CultureInfo.GetCultureInfo(CurrentCulture);

// In WeatherService
private readonly IFormatProvider _fmt;
public WeatherService(..., II18nService i18n)
{
    _fmt = i18n?.CurrentFormatProvider ?? CultureInfo.InvariantCulture;
}
```

This eliminates the conditional branches based on `i18n.CurrentCulture.StartsWith("pt")` and removes the two `new CultureInfo` allocations.

---

## A-06 [Optional] Static `HttpClient` per service

**Files:** `ApiService.cs`, `AirQualityService.cs`, `GeocodingService.cs`, `IpLocationService.cs`
**Severity:** Optional (architectural debt; not a regression)

Each service has its own `private static readonly HttpClient s_httpClient = new() { Timeout = TimeSpan.FromSeconds(5) }`. This is a known .NET trade-off (static client avoids socket exhaustion but loses per-service config). It works fine for a single-process desktop app and is the project's stated convention — but it's worth knowing that:

- All four clients share `Timeout = 5s`, which is a hard field (not via `CancellationTokenSource`). Adding per-call cancellation requires changing the field to a `SocketsHttpHandler`-based `HttpClient` with a `CancellationToken`-aware timeout — that's an `IHttpClientFactory`-shaped migration.

**Remedy:** Don't migrate now. **File a separate decision ticket** ("Migrate to `IHttpClientFactory`") and continue using static `HttpClient`. When the ticket is picked up, do it as a single self-contained change with a perf comparison (test fixture exercising 1000 sequential calls, measure throughput).

---

## A-07 [Optional] Models are mutable POCOs

**Files:** `WinuiWheaterForecastTray.Core/Models/*.cs`
**Severity:** Optional (correctness; also see C-05)

`CurrentWeatherInfo`, `HourlyForecastItem`, `WeatherForecastData` all expose public setters. After `WeatherService.BuildForecast` constructs them, the UI can mutate state behind the model's back.

**Remedy:** Convert to `record` (or `init`-only setters). Records give value-equality, immutability, and a cleaner `ToString` for debugging — all useful for view-models.

```csharp
public sealed record CurrentWeatherInfo
{
    public string CityName { get; init; } = "São Paulo";
    public double Temperature { get; init; }
    ...
}
```

This is a **breaking change** to the public API surface; do it as a focused PR with updated tests.

---

## Verification after this wave

```powershell
dotnet restore WinuiWheaterForecastTray.slnx
dotnet test  WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release
dotnet build WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -p:Platform=x64
```

Run a **full file count check** on the touched services. The fix should reduce the largest service file (`WeatherService.cs`) by ~30% and split it across at most 4 helpers — the goal is that a reader holds one concept at a time.

For A-01, ensure `WeatherHelperTests.cs` and any I18n test in the suite both pass and cover at least one weather code per branch.

For A-02, the fallback chain (native → IP) should be testable in isolation: a `CoordinateProviderChainTests` with two mock providers.
