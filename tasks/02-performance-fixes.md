# 02 — Performance Fixes

Required performance wins identified by the panoramic review.
**Land these after `01-correctness-fixes.md`.** All items here are reversible micro-changes — they don't reshape the codebase.

---

## P-01 [Required] Sequential awaits in `WeatherService.GetForecastAsync`

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs:~60-70`
**Severity:** Required (3x latency on the hot path)

`WeatherService.GetForecastAsync` calls `_apiService`, `_airQualityService`, and `_geocodingService` sequentially. With the shared `HttpClient` `Timeout = 5s`, the worst-case call is up to **15s**. The three calls are independent (AQI and geocoding don't depend on the weather DTO).

**Remedy:** Use `Task.WhenAll`. Expected worst-case latency: ~5s instead of ~15s.

```csharp
var weatherTask = _apiService.GetWeatherDataAsync(lat, lon, cancellationToken);
var aqiTask = _airQualityService?.GetUsAqiAsync(lat, lon, cancellationToken) ?? Task.FromResult<double?>(null);
var cityTask = _geocodingService.GetCityNameAsync(lat, lon, cancellationToken);
await Task.WhenAll(weatherTask, aqiTask, cityTask);
var dto = await weatherTask;
var aqi = await aqiTask;
var city = await cityTask;
```

Verify: existing `WeatherServiceTests` still pass with `Mock<IApiService>`, etc., and the call now issues three concurrent requests. Add a test that asserts all three are *started* (not just one) before the first `await`.

---

## P-02 [Required] Double call to `WeatherHelper.GetWeatherCondition`

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~93-94`
**Severity:** Required (also correctness — see C-07)

Already covered as a correctness fix. Listed here because the second call also wastes CPU on every `GetWeatherCondition` invocation (which is called from the UI every refresh).

**Remedy:** Same as C-07 — call once, deconstruct both tuple elements.

---

## P-03 [Required] Two passes over `dto.Hourly.Time`

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs:~99-112 (precip search) and ~150-169 (start-index search)`
**Severity:** Required (algorithmic)

The hourly data is iterated twice — once to find the matching precipitation slot, once to find the start index. For 168 hourly entries (7 days) this is ~336 loop iterations and array index lookups per call, when one pass would do.

**Remedy:** Merge into a single pass. While iterating `Hourly.Time`, capture both the precip-prob index and the start index in one walk.

```csharp
int precipIndex = -1;
int startIndex = -1;
for (int i = 0; i < dto.Hourly.Time.Count; i++)
{
    if (!DateTime.TryParse(dto.Hourly.Time[i], out var t)) continue;
    if (precipIndex == -1 && t.Date == targetDate && t.Hour == targetHour)
        precipIndex = i;
    if (startIndex == -1 && /* next hour condition */)
    { startIndex = i; break; }
}
```

This also simplifies the dead `else if (dto.Hourly.Time[i].Contains('T'))` branch that parses the same string via slicing.

---

## P-04 [Required] `new CultureInfo(...)` allocated per call

**File:** `WinuiWheaterForecastTray.Core/Services/WeatherService.cs:~95, ~141`
**Severity:** Required (GC pressure on refresh timer)

`new CultureInfo("pt-BR")` is constructed inside `GetForecastAsync` for date formatting. The refresh timer can call this every 15 minutes; not catastrophic but pointless.

**Remedy:** Cache the relevant `CultureInfo` instances as `private static readonly` fields keyed by culture, or — cleaner — pass them via constructor injection from `I18nService`.

```csharp
private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
```

---

## P-05 [Optional] `Math.Round(temperature)` allocation per render

**File:** `WinuiWheaterForecastTray.Core/Models/CurrentWeatherInfo.cs`, `HourlyForecastItem.cs`
**Severity:** Optional

`SummaryText` and `DisplayTemperature` are computed properties that format on every binding access. If bound in a list or data-grid, the formatter runs per cell per render. For the 6-hour forecast list, this is 6 calls — negligible. But the implementation also drops `MidpointRounding` and `CultureInfo.InvariantCulture`, which is a latent correctness issue as well (see `tasks/04-readability-fixes.md`).

**Remedy:** Either materialize on first read (cache), or fix the rounding/culture issue. Don't optimize beyond that unless profiling shows a hot path.

---

## P-06 [Optional] `JsonSerializer.Deserialize` re-parses locale files per construction

**File:** `WinuiWheaterForecastTray.Core/Services/I18nService.cs:~40-55`
**Severity:** Optional

Each `I18nService` instance reads and parses the locale JSON in its constructor. If the lifetime ever becomes per-request (e.g., scoped DI), this becomes wasteful. Today there's one instance per app, so the cost is one-shot.

**Remedy:** Add a static cache keyed by culture:

```csharp
private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cache = new();

private static IReadOnlyDictionary<string, string> LoadFor(string culture)
    => _cache.GetValueOrAdd(culture, LoadFromDisk);
```

Document the cache lifetime (process-lifetime is fine for this app).

---

## Verification after this wave

```powershell
dotnet test WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release --filter "FullyQualifiedName~WeatherService"
dotnet test WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release --filter "FullyQualifiedName~I18nService"
```

Manual smoke test: trigger a refresh with an artificial slow endpoint (set `Task.Delay(4s)` in test mocks for one service). The call should complete in ~4s, not ~12s. Add this as a unit test if practical.
