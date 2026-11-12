# Changelog

All notable changes to SkyTray Weather are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).
Older releases: see [GitHub Releases](https://github.com/tomas-barros1/SkyTray-Weather/releases).

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
