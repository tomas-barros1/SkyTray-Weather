# Changelog

All notable changes to SkyTray Weather are documented here.
Older releases: see [GitHub Releases](https://github.com/tomas-barros1/SkyTray-Weather/releases).

## [1.6.1] - 2026-11-14

### Added
- **Hover card on the tray icon**: resting the mouse on the icon (~0.3 s) opens a WinUI card with an Acrylic background, positioned right above the icon (or below it for a top taskbar) at any DPI. It shows the city and "Updated/Offline" time, current temperature and condition with the SVG icon, feels-like, today's high/low, humidity, wind, chance of rain and the next 4 hours. It never takes focus from the app you are working in, stays open while the cursor is on the icon or the card, and closes shortly after the cursor leaves or when you click the icon.
- **Setting to turn it off** ("Show weather card when hovering the tray icon", in Settings, 5 languages). When off, the classic tooltip is back; when on, the classic tooltip is hidden so the two never appear together.
- Today's high/low (`temperature_2m_max/min`) is now requested from Open-Meteo and stored with the forecast.
- Tests for the show/hide timing rules of the hover card, the high/low mapping and the API request parameters.

### Changed
- The old hover handler that only repositioned the (hidden) panel on every mouse move was replaced by the card logic.
- The "Updated/Offline · updated HH:mm" text is shared between the panel and the card.
- **CI**: the test that calls the real Open-Meteo API is tagged `Integration` and runs in a separate, non-blocking step, so a network error or a rate limit on the shared runner IP can no longer fail the release gate.

### Upgrade notes
- A forecast cached by an older version has no high/low, so the card omits that line until the next successful refresh.
