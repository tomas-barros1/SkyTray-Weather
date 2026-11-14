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

CI in `.github/workflows/build-and-release.yml` follows the same pattern. Tests that call the real network are tagged `[Trait("Category", "Integration")]`; CI runs everything else as the release gate and the `Integration` ones in a separate non-blocking step (`continue-on-error`), so a flaky network or an Open-Meteo rate limit cannot block a release. Any new test that touches the network must carry that trait. Publishing must include `-p:Platform=x64` (or `x86`/`ARM64`); the UI csproj targets all three but requires the platform flag explicitly.

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

## Weather icons

- Icons are a closed set: the `WeatherIcon` enum (Core) ↔ files in `WinuiWeatherForecastTray/Assets/WeatherIcons` (`svg/<name>.svg` for the panel, `png/{16,20,24,32}/<name>.png` for the tray). `WeatherIcon.ToFileName()` is the only place that maps enum → file name, and `WeatherIconAssetsTests` fails if a file is missing. The mapping WMO code → icon lives in `WeatherHelper.GetIcon`.
- Panel: `WeatherIconSource.Get(icon)` returns a cached `SvgImageSource` loaded from the file through a stream (no `ms-appx:` dependency in this unpackaged app). It is also used from `x:Bind` in the hourly list. UI thread only.
- Tray: `TrayIconHelper.CreateIcon(icon)` loads the PNG matching `GetSystemMetricsForDpi(SM_CXSMICON)` via GDI+ and falls back to the older `TrayVectorRenderer` drawing (`CreateEmojiIcon`) if the file is missing. Keep `TrayIconHelper.AssetSizes` and the test's size list in sync with the folders.
- `HourlyForecastItem.Icon` is persisted **by name** (`JsonStringEnumConverter`) so the forecast cache survives enum reordering; `WeatherIcon.Unknown` (0) is what caches written before icons existed deserialize to.
- Adding an icon: add the enum value + `ToFileName` case, the SVG, the four PNG sizes (render the SVG), and the `GetIcon` mapping. Emoji are still used for text only (tooltip summary, metric labels in the panel).
- Source SVGs use only paths, shapes, linear/radial gradients and transforms (no masks/filters/CSS) so WinUI's `SvgImageSource` renders them like a browser does.

## Tray hover card

- Files: `HoverCardWindow` (XAML window), `HoverCardController` (mouse/timer glue), `HoverIntentTracker` (Core, pure logic + unit tests), `WeatherText` (labels shared with the panel). `MainWindow` owns the controller and feeds it `Update(data)`, `NotifyHover()` (tray `WM_MOUSEMOVE`) and `Dismiss()` (click, panel opened, exit).
- **Never put show/hide decisions in the controller**: they belong in `HoverIntentTracker` (300 ms rest before showing, 350 ms grace before hiding, injectable clock) so they stay testable. The tray only reports "mouse moved over the icon", never "mouse left", hence the 100 ms polling `DispatcherQueueTimer` that checks the cursor against the icon rectangle (`Shell_NotifyIconGetRect`, via `TrayIconManager.TryGetIconRect`) and the card bounds. If the shell cannot report the rectangle the controller falls back to "within 24 px of where hovering started".
- The card window must never take focus: it is a tool window with `WS_EX_NOACTIVATE` and is placed with `SetWindowPos(... SWP_NOACTIVATE | SWP_SHOWWINDOW)`. Do not call `Activate()` on it. Size is in DIPs (`WidthDips`/`HeightDips`) and scaled by monitor DPI like the other popups.
- While the card is enabled the classic tooltip is turned off (`TrayIconManager.SetClassicTooltipEnabled(false)` sends an empty `szTip`), otherwise both would show. The setting is `ISettingsService.GetHoverCardEnabled()` (registry value `HoverCardEnabled`, default on), toggled from the settings window; turning it off restores the classic tooltip.
- `CurrentWeatherInfo.TemperatureMax/Min` (nullable) come from the `daily=temperature_2m_max,temperature_2m_min` request parameters; the card hides the high/low line when they are null (e.g. a cache written by an older version).

## Tray context menu

- Right click on the icon raises `TrayIconManager.TrayIconRightClicked`; `MainWindow` shows `TrayMenuWindow` (WinUI, Settings / Exit) next to `TrayIconManager.GetAnchorRect()`. **Shift + right click** still opens the classic native `TrackPopupMenuEx` menu (`ShowContextMenu`): keep it, it is the safety net that guarantees Settings/Exit stay reachable if the WinUI menu misbehaves on some machine.
- Unlike the hover card, the menu **does** take focus on purpose (`Activate()` + `SetForegroundWindow`): it closes on `Deactivated` (click elsewhere, Alt+Tab) and on Esc. Do not give it `WS_EX_NOACTIVATE`, or it would never close when the user clicks away.
- Placement for the hover card and the menu is `PopupPlacement.Calculate` (Core, unit-tested): centered on the icon, above it for a bottom taskbar (below for a top one), clamped into the work area, so a popup never overlaps the taskbar. Add new tray popups through it instead of repeating the maths.
- The menu reuses the `Settings` / `Exit` i18n keys; icons are Segoe Fluent Icons glyphs (`E713` settings, `E7E8` power) with a Segoe MDL2 fallback, not emoji.
