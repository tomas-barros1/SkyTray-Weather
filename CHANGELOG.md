# Changelog

All notable changes to SkyTray Weather are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).
Older releases: see [GitHub Releases](https://github.com/tomas-barros1/SkyTray-Weather/releases).

## [1.7.0] - 2026-11-13

### Added
- **Hover card on the tray icon**: resting the mouse on the icon (~0.3 s) opens a WinUI card with an Acrylic background, positioned right above the icon (or below it for a top taskbar) at any DPI. It shows the city and "Updated/Offline" time, current temperature and condition with the new SVG icon, feels-like, today's high/low, humidity, wind, chance of rain and the next 4 hours. It never takes focus from the app you are working in, stays open while the cursor is on the icon or the card, and closes shortly after the cursor leaves or when you click the icon.
- **Setting to turn it off** ("Show weather card when hovering the tray icon", in Settings, 5 languages). When off, the classic tooltip is back; when on, the classic tooltip is hidden so the two never appear together.
- Today's high/low (`temperature_2m_max/min`) is now requested from Open-Meteo and stored with the forecast.
- Tests: show/hide timing rules of the hover card, high/low mapping, and the API request parameters.

### Changed
- The old hover handler that only repositioned the (hidden) panel on every mouse move was replaced by the card logic.
- The "Updated/Offline · updated HH:mm" text is shared between the panel and the card (`WeatherText`).

### Upgrade notes
- A forecast cached by an older version has no high/low, so the card omits that line until the next successful refresh.

## [1.6.0] - 2026-11-12

### Added
- **Custom weather icon set (SVG)**: 11 icons designed for SkyTray (clear day/night, partly cloudy day/night, cloudy, fog, rain, thunderstorm, snow, loading, error), with a dark rim so they read on both light and dark backgrounds.
- The panel's current-condition icon and the hourly forecast now use the SVG icons instead of emoji.
- **Tray icon matches your display scale**: the tray loads a PNG pre-rendered at the system small-icon size (16/20/24/32 px for 100/125/150/200%) instead of drawing 32 px and letting Windows downscale it. If an asset is missing, the previous GDI+ vector drawing is used as a fallback.
- Tests: code→icon mapping, assets present for every icon/size, and icon persistence in the forecast cache.

### Changed
- Overcast (WMO 3) now has its own "cloudy" icon instead of "partly cloudy", and fog (45/48) its own "fog" icon instead of "rain".
- Emoji remain only in text (tooltip summary and the metric labels in the panel).

### Upgrade notes
- The installer/zip now include `Assets\WeatherIcons\` next to the executable. A forecast cached by an older version has no hourly icons, so those slots show no icon until the next successful refresh.

## [1.5.2] - 2026-11-12

### Fixed
- **Tray icon could be missing for the whole session**: if the first registration failed (taskbar not ready right after login) it was never retried, and the icon was lost whenever Explorer restarted. The icon is now re-added on `TaskbarCreated` and on the next update.
- **Panel cut off on scaled displays**: window sizes were applied in physical pixels, so at 125%/150% scaling the content was clipped. Sizes are now defined in DIPs and scaled to the monitor DPI (also used for positioning near the tray).
- **No way to retry after a failed first load**: the error state now has a "Try again" button (the refresh button was hidden together with the content).
- **"Clear cache" left the saved forecast behind**, so an offline start could show the previous location's weather. It now clears both caches and refreshes immediately.
- **IP geolocation hardening**: coordinates from IP providers are validated (missing, NaN or out-of-range values are rejected and never cached), and the cleartext-HTTP fallback (`ip-api.com`) was replaced by **ipwho.is over HTTPS**.

### Changed
- Window setup shared by the weather panel and the settings window moved to `PopupWindowHelper` (removes duplicated code).
- README lists ipwho.is in the data sources; `AGENTS.md` documents the DPI, tray and location rules.

## [1.5.1] - 2026-11-11

### Changed
- **Now targets .NET 10 (LTS, supported until November 2028)** instead of .NET 8, which reached end of support on 2026-11-10. The Core, UI and Test projects use `net10.0-windows10.0.19041.0` and CI builds with the .NET 10 SDK.
- Release packages are still self-contained: no .NET runtime needs to be installed to run SkyTray Weather.
- Docs (README badge, `AGENTS.md`) and the install script's publish-folder fallback updated for the new target framework.

### Added
- `CHANGELOG.md`.

### Upgrade notes
- Building from source now requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## [1.5.0] - 2026-11-11

### Added
- **Offline mode**: the last successful forecast is saved to `%LocalAppData%\SkyTrayWeather\forecast_cache.json` and shown when a refresh fails (up to 6 hours old), marked as "Offline · updated HH:mm".
- **"Updated HH:mm" label** in the panel header, translated into all five languages (pt_BR, en_US, es_ES, fr_FR, de_DE).
- **Automatic retries** for transient network/server errors (network failures, timeouts, 5xx, 408, 429) when calling Open-Meteo.
- **Location fallback**: when native geolocation and IP lookup both fail, the app now uses the last known coordinates instead of defaulting to São Paulo.
- **Explicit error state** when the very first load fails and nothing is cached (tray icon shows ⚠️ with a message).
- Installer (`SkyTrayWeather-Setup-x64.exe`) and `Install-SkyTray.ps1` now remove files left by the previous, misspelled executable name.
- New unit tests: forecast cache, offline fallback decorator, HTTP retry policy, location fallback.

### Fixed
- Tray icon and tooltip no longer stay stuck on the "loading" icon after a failed refresh.
- Periodic refreshes no longer blank the panel with a spinner; overlapping refreshes (timer + manual + startup retry) are ignored.
- "Start with Windows" entry is re-pointed to the current executable on startup and the legacy entry is removed, so autostart survives upgrades.

### Changed
- **Renamed `Wheater` → `Weather`** in the solution, projects, namespaces and CI. Existing settings are preserved (the legacy registry key name was kept on purpose).
- README: installer `.exe` is now the recommended install method; rain is documented as probability (%), UV as the day's maximum; all five languages are listed.
- `AGENTS.md` updated to match the current code (removed outdated statements, documented offline/legacy-name decisions).
- Panel window height increased slightly (510 → 524 px) to fit the new header line.

### Upgrade notes
- Run the installer over the existing install (or `Install-SkyTray.ps1`), then launch the app once so the autostart entry is refreshed.
- Release assets are published on `v*` tags, so the tag must be `v1.5.0` (not `1.5.0`).
