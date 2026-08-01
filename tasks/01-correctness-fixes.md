# 01 — Correctness Fixes

Findings on correctness and security, ordered by severity.
**Fix every Required item in this file before moving to `02-performance-fixes.md`.**

> Cross-cutting smell that touches everything below: most services use empty `catch {}` blocks with no logging. This is the dominant correctness/observability issue in the codebase. See the architecture wave for a structural remedy; here we fix only the specific defects each catch is hiding.

---

## C-01 [Critical] `IpLocationService` fallback URL is semantically wrong

**File:** `WinuiWheaterForecastTray.Core/Services/IpLocationService.cs:35-39`
**Severity:** Critical (silent broken functionality)

The primary endpoint is `ipapi.co/json/` (IP geolocation — correct). The "fallback" URL inside the inner `catch` is `https://api.bigdatacloud.net/data/reverse-geocode-client`. That endpoint is **reverse-geocoding** (coordinates → address), not IP geolocation. This is almost certainly a copy-paste from `GeocodingService`.

```csharp
// Current (broken)
try { ... ipapi.co ... }
catch
{
    // Try secondary IP geolocation fallback
    var response = await s_httpClient.GetFromJsonAsync<...>(
        "https://api.bigdatacloud.net/data/reverse-geocode-client?..."); // ← wrong
}
```

**Remedy:** Replace the fallback URL with BigDataCloud's IP geolocation endpoint (e.g. `https://api.bigdatacloud.net/data/ip-geolocation-client`). Verify the new endpoint actually returns lat/lon for the calling IP and adjust the DTO accordingly.

If no reliable IP-geolocation fallback exists, **delete the fallback entirely** rather than silently calling the wrong endpoint. A user with no IP geolocation is no worse off than a user who hit a half-broken fallback.

---

## C-02 [Critical] `IpLocationService` rejects (0, 0) coordinates

**File:** `WinuiWheaterForecastTray.Core/Services/IpLocationService.cs:~28`
**Severity:** Critical (silent broken functionality)

```csharp
if (Latitude != 0 && Longitude != 0) ...
```

The (0, 0) point is in the Gulf of Guinea. Users actually located near there are rejected. The check is a sentinel for "endpoint returned garbage" but uses a value that is a legitimate geographic location.

**Remedy:** Replace with explicit field check. The DTO should expose a `Success` or `Status` field, or wrap the result in a nullable record. If the upstream `ipapi.co` response has no explicit success flag, parse `error`/`reason` JSON fields instead.

```csharp
// Pseudocode
if (dto is { Latitude: var lat, Longitude: var lon } && dto.Error is null)
    return (lat, lon);
```

---

## C-03 [Critical] `LocationService` dereferences nullable chain without guards

**File:** `WinuiWheaterForecastTray.Core/Services/LocationService.cs:~40-50`
**Severity:** Critical (NRE risk)

```csharp
position.Coordinate.Point.Position.Latitude
position.Coordinate.Point.Position.Longitude
```

Three chained indirections, all nullable in the WinRT contract. No `?.` anywhere. If the OS returns a partial `Geoposition` (intermittent in real Windows builds when location permission is denied or hardware is failing) the app NREs.

**Remedy:** Add null guards. Either throw a typed `LocationUnavailableException` or return `null` (the contract is already `Task<(double, double)?>`).

```csharp
var pos = position?.Coordinate?.Point?.Position;
if (pos is null) return null;
return (pos.Latitude, pos.Longitude);
```

---

## C-04 [Required] `I18nService` routes `pt-PT` to `pt_BR.json`

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~30`
**Severity:** Required (real localization bug)

```csharp
var culture = ...StartsWith("pt") ? "pt_BR" : ...StartsWith("en") ? "en_US" : "en_US";
```

A Portuguese-from-Portugal user is force-fed Brazilian Portuguese strings.

**Remedy:** Either (a) load a real `pt_PT.json` if/when added and route `pt-PT` to it, or (b) explicitly document the convention and stop pretending to be locale-aware. At minimum, **change the routing** so that the *default* dictionary is `en_US` and only `pt-*` (without region matching Portugal) goes to `pt_BR`. If a `pt_PT.json` resource is added later, the lookup order should be `pt_PT → pt_BR → en_US`.

---

## C-05 [Required] `WeatherService` returns "defaulted" data when API fails

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs:~50-60`
**Severity:** Required (silent broken functionality)

`GetForecastAsync` builds a `WeatherForecastData` even when `dto.Current` is `null`. The caller (UI) cannot tell a real forecast from a placeholder.

**Remedy:** Throw or return a discriminated result. Two acceptable shapes:

- Throw `InvalidOperationException("No current weather returned")` and let `MainWindow` show an error state.
- Change `WeatherForecastData` to be a discriminated union (`{ Ok: ... } | { Error: string }`).

Pick one and apply consistently. Do not silently return defaults that look like data.

---

## C-06 [Required] Mixed `DateTime.Now` and API-derived time in same call

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs:~85-93, ~140`
**Severity:** Required (subtle correctness)

The precip-prob lookup uses the `DateTime` parsed from `dto.Current.Time`. The date string formatter and the hourly-search target use `DateTime.Now`. Mixing UTC-derived API time with local clock time can desync at hour boundaries or after DST changes.

**Remedy:** Pick one source of truth. The cleanest fix: derive `now` from `dto.Current.Time` (parsed once at the top), and use it everywhere within the call. Drop `DateTime.Now`.

---

## C-07 [Required] `I18nService.GetWeatherCondition` calls `WeatherHelper.GetWeatherCondition` twice

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~93-94`
**Severity:** Required (correctness + redundant work)

```csharp
var (defaultEmoji, _) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);
var (_, defaultDescription) = WeatherHelper.GetWeatherCondition(weatherCode, isDay); // second call
```

`WeatherHelper.GetWeatherCondition` is pure and idempotent — calling it twice is wasteful and the discard of the first tuple is a code smell.

**Remedy:**

```csharp
var (defaultEmoji, defaultDescription) = WeatherHelper.GetWeatherCondition(weatherCode, isDay);
```

---

## C-08 [Required] Inconsistent fallback semantics across services

**File:** `WinuiWheaterForecastTray.Core/Services/ApiService.cs`, `AirQualityService.cs`, `GeocodingService.cs`, `IpLocationService.cs`
**Severity:** Required (consistency)

`ApiService` throws on failure (propagates exception). The other three swallow and return defaults (`42.0`, `"São Paulo"`, `null`). Callers cannot write one error-handling path; they have to remember which service swallows.

**Remedy:** Pick a project-wide convention. Recommended: **all services throw** on network/parse failure, and the orchestrator (`WeatherService`) decides how to degrade (e.g., AQI returns `null` if the call fails; the UI shows "—" instead of `42.0`). Update tests accordingly.

If a quick fix is preferred, document the inconsistency in `IWeatherService` XML docs and accept it as intentional.

---

## C-09 [Required] `GeocodingService` magic-fallback "São Paulo"

**File:** `WinuiWheaterForecastTray.Core/Services/GeocodingService.cs:~35`
**Severity:** Required (correctness; user-visible bug)

When the geocoding call fails, `GetCityNameAsync` returns the hardcoded literal `"São Paulo"`. An English-locale user in New York sees "São Paulo, 22°C" on a real failure.

**Remedy:** Either:

- Return `string?` (or an empty string) on failure and let the UI show a "—" placeholder.
- Take a `string locale` parameter and pass through from `I18nService.CurrentCulture` so the language preference is correct.

At minimum, **do not return a city name that wasn't actually geocoded**. The current behavior is misleading at best and could be a privacy-adjacent issue at worst (displaying a wrong location for a sensitive user).

---

## C-10 [Required] `LocationService` returns same `null` for timeout, denied, exception

**File:** `WinuiWheaterForecastTray.Core/Services/LocationService.cs:~30-50`
**Severity:** Required (UX; debuggability)

The two `catch` blocks both return `null`. A user denied location permission and a user whose location service timed out are indistinguishable.

**Remedy:** Wrap `null` in a discriminated result. Either:

```csharp
public enum LocationFailure { PermissionDenied, Timeout, Unavailable }
public record LocationResult(double Latitude, double Longitude, LocationFailure? Failure);
```

or three distinct nullable results. Even just `Debug.WriteLine` with the failure reason (which `LocationService` already does) is enough to debug; the bigger problem is that the caller cannot react differently to "denied" vs "timeout".

---

## C-11 [Required] `AutostartService.SetAutostart` writes unescaped path

**File:** `WinuiWheaterForecastTray.Core/Services/AutostartService.cs:~40`
**Severity:** Required (security/correctness edge case)

The path is wrapped in `"..."` but if `Environment.ProcessPath` contains a literal `"` (unusual but possible), the registry value is corrupted. Realistic only in adversarial edge cases, but cheap to defend.

**Remedy:** Validate `exePath` contains no `"` before writing, or use `RegSetValueEx` with proper escaping. For a single-quoted registry value, the simplest guard is:

```csharp
if (exePath.Contains('"')) throw new ArgumentException("Exe path contains invalid characters.");
```

---

## C-12 [Optional] `SettingsService.GetRefreshIntervalMinutes` silently returns default on registry corruption

**File:** `WinuiWheaterForecastTray.Core/Services/SettingsService.cs:~30`
**Severity:** Optional (resilience)

If the registry value is somehow a non-int (e.g., user edited regedit), `val is int minutes` is `false` and the default `15` is returned. The user sees "15 minutes" with no warning that their stored value was unreadable.

**Remedy:** Log via `Debug.WriteLine` or surface a one-time UI warning. Not critical; the user can manually re-set the value.

---

## C-13 [Optional] `SettingsService.SetRefreshIntervalMinutes` accepts unbounded values

**File:** `WinuiWheaterForecastTray.Core/Services/SettingsService.cs:~40`
**Severity:** Optional (defensive validation)

The setter only checks `> 0`. `9999999` is accepted and produces a 6,944-day refresh interval. The UI prevents this today via a fixed dropdown, but the public API doesn't.

**Remedy:** Either validate the bound (`1 <= minutes <= 1440`) or document the caller-side invariant in XML doc.

---

## C-14 [Optional] JSON deserialization is case-sensitive but lookup uses OrdinalIgnoreCase

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~50`
**Severity:** Optional (latent fragility)

`JsonSerializer.Deserialize<Dictionary<string, string>>(json)` uses default options (case-sensitive). The dictionary is then stored with `OrdinalIgnoreCase`. This works **only because all current keys are lowercase**. If anyone adds `"WeatherCode"` (PascalCase) the deserializer succeeds but every lookup misses silently.

**Remedy:** Add `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }` or normalize keys at load. Document the lowercase-only invariant if you want to keep the current behavior.

---

## Test gaps to address (Required)

After fixing the above, add **regression tests** for each of:

| Finding | Test |
|---|---|
| C-01 | `IpLocationServiceTests` — fallback URL returns lat/lon for an IP-shaped response |
| C-02 | `IpLocationServiceTests` — `(0, 0)` response is *accepted* (or rejected by an explicit success flag) |
| C-03 | `LocationServiceTests` — partial `Geoposition` returns `null` (or throws) instead of NREing |
| C-04 | `I18nServiceTests` — `"pt-PT"` routes to `pt_PT` (or `en_US` fallback with explicit log) |
| C-05 | `WeatherServiceTests` — `dto.Current == null` throws or returns `Error` |
| C-06 | `WeatherServiceTests` — same clock is used throughout one call (verify by passing a fake `DateTime` provider or capturing `dto.Current.Time` and asserting all derived times match) |
| C-08 | `WeatherServiceTests` — failure of `IApiService` propagates; failure of `IAirQualityService` returns `null` AQI, not `42.0` |
| C-09 | `GeocodingServiceTests` — failure returns `string?` or `""`, not `"São Paulo"` |

These tests are required to land the corresponding fixes.

---

## Verification after this wave

```powershell
dotnet restore WinuiWheaterForecastTray.slnx
dotnet test  WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release
```

Confirm:
- All new regression tests pass.
- No existing tests regress.
- `dotnet build WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -p:Platform=x64` succeeds.
- `WinuiWheaterForecastTray.Core` builds with `<Nullable>enable</Nullable>` (no new nullability warnings).
