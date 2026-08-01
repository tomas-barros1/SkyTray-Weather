# 04 — Readability Fixes

Polish and consistency items. Lower priority than correctness/performance/architecture — these don't block merge but compound over time if left.
**Land after `03-architecture-fixes.md`.**

---

## R-01 [Required] Magic strings duplicated between model and service

**Files:**
- `WinuiWheaterForecastTray.Core/Models/CurrentWeatherInfo.cs` (default values: `"São Paulo"`, `"Bom"`, `"Moderado"`, `"05:55"`, `"17:30"`)
- `WinuiWheaterForecastTray.Core/Services/WeatherService.cs` (the same strings as fallbacks)

**Severity:** Required (drift risk)

The defaults `"05:55"` and `"17:30"` appear in both files. If you change one, the other silently drifts. Same for `"Bom"` and `"Moderado"` (portuguese fallbacks for AQI/UV labels).

**Remedy:** Centralize. Two options:

- Move defaults into `WeatherService` (the orchestrator knows what to put there) and remove the model-level defaults — models become plain POCOs.
- Extract a `static class CurrentWeatherDefaults` referenced by both.

Pick **option 1** (push defaults to orchestrator). The model should be free of magic strings; the service owns the policy.

---

## R-02 [Required] Hardcoded URLs scattered across services

**Files:**
- `WinuiWheaterForecastTray.Core/Services/ApiService.cs:~18` (`const string BaseUrl`)
- `WinuiWheaterForecastTray.Core/Services/AirQualityService.cs:~20` (literal in method)
- `WinuiWheaterForecastTray.Core/Services/GeocodingService.cs:~25` (literal in method)
- `WinuiWheaterForecastTray.Core/Services/IpLocationService.cs:~30, ~37` (literals in method)

**Severity:** Required (consistency)

`ApiService` has `private const string BaseUrl`. The other three inline the URL in their fetch methods. Search-and-replace or environment override is harder than it needs to be.

**Remedy:** Add a single `static class EndpointUrls` (or constants on each interface) and reference from each service:

```csharp
internal static class EndpointUrls
{
    public const string OpenMeteoForecast = "https://api.open-meteo.com/v1/forecast";
    public const string OpenMeteoAirQuality = "https://air-quality-api.open-meteo.com/v1/air-quality";
    public const string BigDataCloudReverseGeocode = "https://api.bigdatacloud.net/data/reverse-geocode-client";
    public const string BigDataCloudIpGeolocation = "https://api.bigdatacloud.net/data/ip-geolocation-client";
    public const string IpApi = "https://ipapi.co/json/";
}
```

---

## R-03 [Required] Class modifier inconsistency (`sealed` vs not)

**Files:**
- `WinuiWheaterForecastTray.Core/Services/AutostartService.cs` (not `sealed`)
- `WinuiWheaterForecastTray.Core/Services/SettingsService.cs` (not `sealed`)

**Severity:** Required (consistency)

All other services are `sealed`. The two exceptions are registry helpers with no inheritance use case.

**Remedy:** Add `sealed` to both. Per AGENTS.md: "New services should be `sealed` unless there's a reason."

```csharp
public sealed class AutostartService : IAutostartService { ... }
public sealed class SettingsService : ISettingsService { ... }
```

---

## R-04 [Required] No XML doc on public APIs

**Files:** all public service interfaces and classes
**Severity:** Required (DX; consistency)

`LocationService` has a class `<summary>` but no method docs. The other services have nothing. Tests reference members by behavior — that's fine, but consumers (and future maintainers) need at least one-line docs on every public method.

**Remedy:** Add `<summary>` to every public method and class. Minimal examples:

```csharp
/// <summary>
/// Fetches the current US AQI for the given coordinates. Returns <c>null</c> on failure.
/// </summary>
public Task<double?> GetUsAqiAsync(double latitude, double longitude, CancellationToken ct = default);
```

Don't go beyond one-line summaries — the project doesn't use verbose XML docs elsewhere; this is about parity, not expansion.

---

## R-05 [Required] Magic numbers without names

**Files:**
- `WinuiWheaterForecastTray.Core/Services/LocationService.cs:~35` (`DesiredAccuracyInMeters = 500`)
- `WinuiWheaterForecastTray.Core/Services/LocationService.cs:~25` (`CancelAfter(5s)`)
- `WinuiWheaterForecastTray.Core/Services/AirQualityService.cs:~25` (`42.0` default AQI)
- `WinuiWheaterForecastTray.Core/Services/WeatherService.cs` (`3.0` UV default, `6` hourly slots, etc.)

**Severity:** Required (maintainability)

The `500` accuracy constant and `5s` timeout are reasonable values but unnamed. The `42.0` AQI default and `3.0` UV default look arbitrary and inviting.

**Remedy:** Name them.

```csharp
private const int LocationAccuracyMeters = 500;
private static readonly TimeSpan LocationTimeout = TimeSpan.FromSeconds(5);
private const double DefaultUsAqi = 42.0;        // TODO: see C-08 — should be removed
private const double DefaultUvIndex = 3.0;
private const int HourlyForecastHours = 6;
```

The defaults for AQI/UV deserve a `TODO:` marker pointing back to `01-correctness-fixes.md` C-08 — if the failure semantics are unified (throw, not default), these constants disappear.

---

## R-06 [Required] `AutostartService` references its interface via fully-qualified name

**File:** `WinuiWheaterForecastTray.Core/Services/AutostartService.cs:~7`
**Severity:** Required (consistency)

```csharp
public class AutostartService : Interfaces.IAutostartService
```

Every other service uses `using Services.Interfaces;` plus `IAutostartService`. The fully-qualified form here is inconsistent and visually noisy.

**Remedy:** Add the `using` and shorten to `IAutostartService`. While you're at it, add `sealed` per R-03.

---

## R-07 [Optional] Inconsistent `using` ordering

**Files:** service files
**Severity:** Optional (style)

Some services use `System.*` before `WinuiWheaterForecastTray.*`, others the reverse. StyleCop default sort puts `System.*` first.

**Remedy:** Configure `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` and `<EnableNETAnalyzers>true</EnableNETAnalyzers>` in `WinuiWheaterForecastTray.Core.csproj`. Don't manually re-sort every file — let the IDE/compiler enforce it.

---

## R-08 [Optional] `WeatherHelper` hardcoded English descriptions

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherHelper.cs`
**Severity:** Optional (i18n drift)

`WeatherHelper.GetWeatherCondition` returns English descriptions like `"Sunny"`, `"Foggy"`, `"Rain"`. These surface as fallback when `I18nService.GetString` misses. They're English even when the user's locale is Portuguese.

**Remedy:** Either rename `DefaultDescription` to `FallbackDescription` and document that it is English-only fallback, or move the English strings to `en_US.json` as the canonical fallback for the english culture. Either is fine; the bug is that the convention isn't named.

---

## R-09 [Optional] `I18nService.FormatSummaryText` uses emojis as literals

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~110-120`
**Severity:** Optional (i18n)

The summary template embeds `📍`, `💧`, `🌬️` directly. These would benefit from being moved to the locale JSON so translators can adjust them.

**Remedy:** Move the format string into `Resources/{culture}.json` under a `summary.format` key. The `FormatSummaryText` method then becomes a thin wrapper:

```csharp
public string FormatSummaryText(...) => string.Format(GetString("summary.format", DefaultSummaryFormat), args);
```

This is a structural improvement but not blocking.

---

## R-10 [Optional] `I18nService.GetString` silently returns the key

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~60-70`
**Severity:** Optional (DX)

When a key is missing, the method returns `key` itself (or the explicit fallback). In production, a typo like `weather.Sunnny` (extra n) silently displays the literal `"weather.Sunnny"` to the user.

**Remedy:** Log the missing key in debug builds (similar to R-05 / A-04 pattern):

```csharp
public string GetString(string key, string fallback = "")
{
    if (_translations.TryGetValue(key, out var value)) return value;
    _logger?.LogDebug("Missing i18n key '{Key}' for culture '{Culture}'", key, CurrentCulture);
    return fallback.Length > 0 ? fallback : key;
}
```

---

## R-11 [Optional] Models have no XML docs

**Files:** `WinuiWheaterForecastTray.Core/Models/*.cs`
**Severity:** Optional (DX)

`CurrentWeatherInfo.PrecipitationProbability` has a `<summary>`; nothing else does.

**Remedy:** Add one-line summaries to every public property. Same rule as R-04 — parity, not expansion.

---

## Verification after this wave

```powershell
dotnet restore WinuiWheaterForecastTray.slnx
dotnet test  WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release
dotnet build WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -p:Platform=x64 /p:TreatWarningsAsErrors=true
```

The last command (`TreatWarningsAsErrors=true`) ensures no new nullability warnings, no new analyzer warnings, and no new CS1591 (missing XML doc on public API) warnings creep in. If you don't want full strict-mode, at least confirm `<Nullable>enable</Nullable>` produces a clean build.
