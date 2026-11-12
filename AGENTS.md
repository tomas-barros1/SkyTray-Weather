# AGENTS.md

Compact guidance for OpenCode sessions working in `SkyTray Weather` (WinUI 3 system-tray weather app, .NET 10).

## Repository at a glance

Three .NET projects, no `*.sln` (only the XML-format `WinuiWeatherForecastTray.slnx`). The solution/project names used to be misspelled `Wheater`; that was fixed (see "Legacy names" below):

| Project | Path | Purpose |
|---|---|---|
| `WinuiWeatherForecastTray.Core` | `WinuiWeatherForecastTray.Core/` | Domain library: DTOs, services, models, i18n |
| `WinuiWeatherForecastTray` | `WinuiWeatherForecastTray/` | WinUI 3 unpackaged app, XAML, tray renderer |
| `WinuiWeatherForecastTray.Tests` | `WinuiWeatherForecastTray.Tests/` | xUnit + Moq + FluentAssertions |

All target `net10.0-windows10.0.19041.0` with `<Nullable>enable</Nullable>`. UI app is unpackaged (`WindowsPackageType=None`).

## Solution File

`WinuiWeatherForecastTray.slnx` includes all three projects (`WinuiWeatherForecastTray.Core`, `WinuiWeatherForecastTray`, `WinuiWeatherForecastTray.Tests`). Build, test, and publish commands:

```powershell
dotnet restore WinuiWeatherForecastTray.slnx
dotnet build   WinuiWeatherForecastTray.slnx -p:Platform=x64
dotnet test    WinuiWeatherForecastTray.Tests/WinuiWeatherForecastTray.Tests.csproj -c Release
dotnet publish WinuiWeatherForecastTray/WinuiWeatherForecastTray.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o ./publish
```

CI in `.github/workflows/build-and-release.yml` follows the same pattern. Publishing must include `-p:Platform=x64` (or `x86`/`ARM64`); the UI csproj targets all three but requires the platform flag explicitly.

## Entry points

- App startup: `WinuiWeatherForecastTray/App.xaml.cs` → constructs `MainWindow`.
- Window logic + tray integration: `WinuiWeatherForecastTray/MainWindow.xaml.cs` (~475 lines, mixes WinUI, Win32 P/Invoke, services, timer).
- Settings UI: `WinuiWeatherForecastTray/SettingsWindow.xaml.cs`.
- Tray subsystem: `WinuiWeatherForecastTray/Tray/{TrayIconManager,TrayIconHelper,TrayVectorRenderer}.cs` (GDI+ vector rendering, `NOTIFYICONDATA` shell notifications).
- Win32 interop structs: `WinuiWeatherForecastTray/Native/` (`NOTIFYICONDATA`, `ICONINFO`, `POINT`, `GdipPointI`, `GdipRectF`, `GdiplusStartupInput`).
- Service orchestration: `WinuiWeatherForecastTray.Core/Services/WeatherService.cs` (composes `IApiService`, `ILocationService`, `IGeocodingService`, `IAirQualityService`, `II18nService`).
- i18n resources: `WinuiWeatherForecastTray.Core/Resources/*.json`, copied to output via the `<None Update="Resources\*.json">` rule.

## Conventions that diverge from defaults

- **Nullable is on everywhere.** Don't disable it; do honor it in new files (`string?`, `ArgumentNullException.ThrowIfNull`).
- **No DI container.** Services are constructed and held directly in `MainWindow`. Don't introduce `Microsoft.Extensions.DependencyInjection` without a deliberate migration.
- **Static default `HttpClient`** is the convention in `ApiService`, `AirQualityService`, `GeocodingService`, `IpLocationService` (one shared instance, `Timeout = 5s`). Don't refactor to `IHttpClientFactory` unless you're explicitly migrating.
- **Exception handling:** deliberately swallowed exceptions go through `DebugLog.Swallowed(owner, ex, context)` (Core, `Debug`-only output). There is no `ILogger`; don't add `ILogger<T>` without a broader decision. Don't add new empty `catch {}` blocks.
- **Hardcoded Portuguese fallbacks** exist in models (`CurrentWeatherInfo`, e.g. `"São Paulo"`, `"Bom"`, `"Moderado"`, `"05:55"`, `"17:30"`) and `GeocodingService` returns `"São Paulo"` on miss. Be aware when fixing i18n bugs.
- **`I18nService`** picks the resource by culture prefix (`pt`, `es`, `fr`, `de`, otherwise `en_US`). `pt-PT` intentionally maps to `pt_BR.json` until a `pt_PT.json` exists. New UI strings must be added to **all five** JSON files (the code passes an English fallback to `GetString`, but don't rely on it).
- **WMO weather-code mapping** lives in `WeatherHelper.GetWeatherCondition`; `I18nService.GetWeatherCondition` wraps it and only localises the description.
- **Class modifiers are inconsistent** across services: most are `sealed`, but `AutostartService` and `SettingsService` are not. New services should be `sealed` unless there's a reason.

## Test running

- Suite: xUnit (`2.7.0`), Moq (`4.20.70`), FluentAssertions (`6.12.0`).
- Mock HTTP via `WinuiWeatherForecastTray.Tests/MockHttpMessageHandler.cs`.
- Integration tests in `ApiContractIntegrationTests.cs` exercise `ApiService` against fixture JSON.
- Single-test invocation: `dotnet test WinuiWeatherForecastTray.Tests/WinuiWeatherForecastTray.Tests.csproj --filter "FullyQualifiedName~TestName"`.
- Tests do NOT cover the UI project (no `Microsoft.WindowsAppSDK` test host). Stay in `Core`/`Tests` when writing tests.

## Build artifacts and what to ignore

- `bin/`, `obj/`, `.vs/`, `*.user`, `*.suo` — already in `.gitignore`.
- `.opencode/node_modules/` is opencode's own skill runtime; ignore unless working on skills.
- `WinuiWeatherForecastTray/obj/` and `WinuiWeatherForecastTray.Core/obj/` are generated; never hand-edit.

## Release flow (verified from `.github/workflows/build-and-release.yml`)

1. Trigger: push to `main`, PR to `main`, or tag matching `v*`.
2. Runs on `windows-latest`, .NET SDK `10.0.x`.
3. Steps in order: restore → **test** → publish `win-x64` self-contained → zip with `Install-SkyTray.ps1` → build the Inno Setup installer (`installer.iss`) → upload artifacts (`SkyTray-Weather-win-x64.zip` and `Output/SkyTrayWeather-Setup-x64.exe`, 30-day retention).
4. On `v*` tag push: creates a GitHub Release named `SkyTray Weather Release <tag>` with the zip **and** the installer attached.

To cut a release locally without CI: publish, zip, and upload — see commands above. Output artifacts: `SkyTray-Weather-win-x64.zip` and `SkyTrayWeather-Setup-x64.exe` (the `Install-SkyTray.ps1` next to it copies `%LocalAppData%\SkyTrayWeather` and creates a Start Menu shortcut).

## Things that are easy to miss

- `MainWindow.xaml.cs` defines its own `WndProc` and uses `SetWindowLongPtr` to subclass the window for tray callbacks. Any change to message handling must preserve the chain to `_oldWndProc` or tray interaction breaks silently.
- `I18nService` uses `OrdinalIgnoreCase` on the dictionary but the JSON is parsed with default options (case-sensitive) — the lookup handles it, but JSON keys must remain lowercase.
- The README's data-source table is the canonical license list (Open-Meteo, BigDataCloud, ipapi.co, ipwho.is). When adding a new third-party endpoint, update both the README and the registry constants in `AutostartService` if it writes paths.
- `Math.Round(temperature)` calls in computed properties (`CurrentWeatherInfo.SummaryText`, `HourlyForecastItem.DisplayTemperature`) use the default culture — fine for `pt-BR`/`en-US`, but the decimal separator will vary elsewhere. Use `Math.Round(x, MidpointRounding.AwayFromZero)` with `CultureInfo.InvariantCulture` if touching these.
- The Open-Meteo precipitation display is `precipitation_probability`, not instantaneous mm — the README calls this out explicitly; preserve the contract.

## Offline behaviour and resilience

- `MainWindow` wraps `WeatherService` in `CachedWeatherService` (decorator). Every successful forecast is saved by `ForecastCacheService` to `%LocalAppData%\SkyTrayWeather\forecast_cache.json`; when a fetch fails the last copy (max 6 h old) is returned with `WeatherForecastData.IsStale = true`. The cache is **not keyed by location**, so it is only used for auto-located requests (no explicit lat/lon). `IsStale` is `[JsonIgnore]` and set only on read.
- Computed model properties (`SummaryText`, `DisplayTemperature`, `TooltipText`, obsolete `SurfacePressure`) are `[JsonIgnore]` so the cache file only holds real data. Keep it that way when adding computed properties.
- `ApiService` retries transient failures (network errors, timeouts, 5xx, 408, 429) with delays `400 ms, 1200 ms`; 4xx and malformed JSON are not retried. Pass `retryDelays: Array.Empty<TimeSpan>()` (or zero delays) in tests to avoid waiting.
- `WeatherService` location order: native geolocator → IP → **last cached coordinates** → hard-coded default city (São Paulo).
- `MainWindow.RefreshWeatherAsync` only shows the spinner on the very first load, ignores overlapping refreshes, and shows an explicit error state (tray icon ⚠️) if the first load fails with nothing cached. Do not reintroduce the "tray icon stuck on 🔄 after a failure" behaviour.

## Legacy names (kept on purpose)

The registry key `HKCU\Software\WinuiWheaterForecastTray` (settings) and the legacy `Run` value `WinuiWheaterForecastTray` keep the old misspelling so existing users do not lose settings and old autostart entries are cleaned up. `AutostartService.RefreshAutostartPath()` runs at startup and re-points the `Run` entry to the current executable; `installer.iss` and `Install-SkyTray.ps1` delete files named `WinuiWheaterForecastTray.*` left from older installs.

## Windows, tray and location (review follow-ups)

- **DPI:** `AppWindow.Resize` takes *physical pixels*, XAML works in DIPs. Never hard-code pixel sizes: use `PopupWindowHelper.ResizeToDips(...)` (and read `_appWindow.Size` for positioning). `PopupWindowHelper.ApplyChrome` is the single place for the borderless/always-on-top/backdrop/rounded-corner setup shared by `MainWindow` and `SettingsWindow`.
- **Tray icon:** `TrayIconManager` re-adds the icon when Explorer broadcasts `TaskbarCreated` (`RestoreIcon`, wired in `MainWindow.CustomWndProc`) and `Update` retries `NIM_ADD` if the first add failed (taskbar not ready right after login).
- **First-load failure:** the panel shows the error text plus a "Try again" button (`BtnRetry`); the header with the refresh button lives inside `ContentGrid`, which is collapsed in that state.
- **Clear cache** (Settings) clears the location cache *and* the saved forecast, then `MainWindow` drops its in-memory forecast and refetches (`SettingsWindow.CacheCleared`).
- **IP geolocation:** `ipapi.co` then `ipwho.is`, both HTTPS. Coordinates are untrusted network input: `IpLocationService.ValidateCoordinates` rejects missing (null), NaN and out-of-range values; an explicit `(0, 0)` is still accepted on purpose (see the tests). Never add a cleartext-HTTP provider.
