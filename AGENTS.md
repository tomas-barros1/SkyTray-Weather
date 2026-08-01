# AGENTS.md

Compact guidance for OpenCode sessions working in `SkyTray Weather` (WinUI 3 system-tray weather app, .NET 8).

## Repository at a glance

Three .NET projects, no `*.sln` (only the XML-format `WinuiWheaterForecastTray.slnx`):

| Project | Path | Purpose |
|---|---|---|
| `WinuiWheaterForecastTray.Core` | `WinuiWheaterForecastTray.Core/` | Domain library: DTOs, services, models, i18n |
| `WinuiWheaterForecastTray` | `WinuiWheaterForecastTray/` | WinUI 3 unpackaged app, XAML, tray renderer |
| `WinuiWheaterForecastTray.Tests` | `WinuiWheaterForecastTray.Tests/` | xUnit + Moq + FluentAssertions |

All target `net8.0-windows10.0.19041.0` with `<Nullable>enable</Nullable>`. UI app is unpackaged (`WindowsPackageType=None`).

## Solution File

`WinuiWheaterForecastTray.slnx` includes all three projects (`WinuiWheaterForecastTray.Core`, `WinuiWheaterForecastTray`, `WinuiWheaterForecastTray.Tests`). Build, test, and publish commands:

```powershell
dotnet restore WinuiWheaterForecastTray.slnx
dotnet build   WinuiWheaterForecastTray.slnx -p:Platform=x64
dotnet test    WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj -c Release
dotnet publish WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o ./publish
```

CI in `.github/workflows/build-and-release.yml` follows the same pattern. Publishing must include `-p:Platform=x64` (or `x86`/`ARM64`); the UI csproj targets all three but requires the platform flag explicitly.

## Entry points

- App startup: `WinuiWheaterForecastTray/App.xaml.cs` → constructs `MainWindow`.
- Window logic + tray integration: `WinuiWheaterForecastTray/MainWindow.xaml.cs` (387 lines, mixes WinUI, Win32 P/Invoke, services, timer).
- Settings UI: `WinuiWheaterForecastTray/SettingsWindow.xaml.cs`.
- Tray subsystem: `WinuiWheaterForecastTray/Tray/{TrayIconManager,TrayIconHelper,TrayVectorRenderer}.cs` (GDI+ vector rendering, `NOTIFYICONDATA` shell notifications).
- Win32 interop structs: `WinuiWheaterForecastTray/Native/` (`NOTIFYICONDATA`, `ICONINFO`, `POINT`, `GdipPointI`, `GdipRectF`, `GdiplusStartupInput`).
- Service orchestration: `WinuiWheaterForecastTray.Core/Services/WeatherService.cs` (composes `IApiService`, `ILocationService`, `IGeocodingService`, `IAirQualityService`, `II18nService`).
- i18n resources: `WinuiWheaterForecastTray.Core/Resources/*.json`, copied to output via the `<None Update="Resources\*.json">` rule.

## Conventions that diverge from defaults

- **Nullable is on everywhere.** Don't disable it; do honor it in new files (`string?`, `ArgumentNullException.ThrowIfNull`).
- **No DI container.** Services are constructed and held directly in `MainWindow`. Don't introduce `Microsoft.Extensions.DependencyInjection` without a deliberate migration.
- **Static default `HttpClient`** is the convention in `ApiService`, `AirQualityService`, `GeocodingService`, `IpLocationService` (one shared instance, `Timeout = 5s`). Don't refactor to `IHttpClientFactory` unless you're explicitly migrating.
- **Exception handling:** most services use empty `catch {}` blocks. Only `LocationService` writes via `Debug.WriteLine`. There is no `ILogger`. Don't add `ILogger<T>` without a broader decision; do at least leave a `// TODO:` if you touch a swallow site.
- **Hardcoded Portuguese fallbacks** exist in models (`CurrentWeatherInfo`, e.g. `"São Paulo"`, `"Bom"`, `"Moderado"`, `"05:55"`, `"17:30"`) and `GeocodingService` returns `"São Paulo"` on miss. Be aware when fixing i18n bugs.
- **`I18nService`** uses `culture.StartsWith("pt")` / `StartsWith("en")` — this routes `pt-PT` to `pt_BR.json`. Known bug; treat carefully.
- **WMO weather-code mapping** lives in two places: `WeatherHelper.GetWeatherCondition` and `I18nService.GetWeatherCondition`. The latter calls the former **twice** in a row (discarding the first tuple) — easy follow-up fix.
- **Class modifiers are inconsistent** across services: most are `sealed`, but `AutostartService` and `SettingsService` are not. New services should be `sealed` unless there's a reason.

## Test running

- Suite: xUnit (`2.7.0`), Moq (`4.20.70`), FluentAssertions (`6.12.0`).
- Mock HTTP via `WinuiWheaterForecastTray.Tests/MockHttpMessageHandler.cs`.
- Integration tests in `ApiContractIntegrationTests.cs` exercise `ApiService` against fixture JSON.
- Single-test invocation: `dotnet test WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --filter "FullyQualifiedName~TestName"`.
- Tests do NOT cover the UI project (no `Microsoft.WindowsAppSDK` test host). Stay in `Core`/`Tests` when writing tests.

## Build artifacts and what to ignore

- `bin/`, `obj/`, `.vs/`, `*.user`, `*.suo` — already in `.gitignore`.
- `.opencode/node_modules/` is opencode's own skill runtime; ignore unless working on skills.
- `WinuiWheaterForecastTray/obj/` and `WinuiWheaterForecastTray.Core/obj/` are generated; never hand-edit.

## Release flow (verified from `.github/workflows/build-and-release.yml`)

1. Trigger: push to `main`, PR to `main`, or tag matching `v*`.
2. Runs on `windows-latest`, .NET SDK `8.0.x`.
3. Steps in order: restore → **test** → publish `win-x64` self-contained → zip with `Install-SkyTray.ps1` → upload artifact (`SkyTray-Weather-win-x64.zip`, 30-day retention).
4. On `v*` tag push: creates a GitHub Release named `SkyTray Weather Release <tag>` with the zip attached.

To cut a release locally without CI: publish, zip, and upload — see commands above. Output artifact: `SkyTray-Weather-win-x64.zip` (the `Install-SkyTray.ps1` next to it copies `%LocalAppData%\SkyTrayWeather` and creates a Start Menu shortcut).

## Things that are easy to miss

- The solution file (`*.slnx`) does not include the Core or Tests project. New agents assume `dotnet build` on the slnx will compile everything — it does not.
- `MainWindow.xaml.cs` defines its own `WndProc` and uses `SetWindowLongPtr` to subclass the window for tray callbacks. Any change to message handling must preserve the chain to `_oldWndProc` or tray interaction breaks silently.
- `I18nService` uses `OrdinalIgnoreCase` on the dictionary but the JSON is parsed with default options (case-sensitive) — the lookup handles it, but JSON keys must remain lowercase.
- The README's data-source table is the canonical license list (Open-Meteo, BigDataCloud, ipapi.co). When adding a new third-party endpoint, update both the README and the registry constants in `AutostartService` if it writes paths.
- `Math.Round(temperature)` calls in computed properties (`CurrentWeatherInfo.SummaryText`, `HourlyForecastItem.DisplayTemperature`) use the default culture — fine for `pt-BR`/`en-US`, but the decimal separator will vary elsewhere. Use `Math.Round(x, MidpointRounding.AwayFromZero)` with `CultureInfo.InvariantCulture` if touching these.
- The Open-Meteo precipitation display is `precipitation_probability`, not instantaneous mm — the README calls this out explicitly; preserve the contract.
