# SkyTray weather icons

11 flat icons, 64x64 viewBox, plain SVG (paths, circles, lines, gradients; no masks/filters/CSS) so they render
the same in WinUI `SvgImageSource`, browsers and rasterizers. Clouds have a darker rim so they stay visible on
both light and dark taskbars/backgrounds.

`png/<size>/` has pre-rendered rasters (16, 20, 24, 32, 48, 64 px) for the tray: Windows uses 16 px at 100% scale,
20 at 125%, 24 at 150%, 32 at 200% (pick the size from the DPI instead of always drawing 32 and downscaling).

## Suggested mapping (Open-Meteo / WMO weather codes)

| WMO code | Meaning | Icon |
|---|---|---|
| 0 | Clear sky | `clear-day` / `clear-night` (by `is_day`) |
| 1, 2 | Mainly clear, partly cloudy | `partly-cloudy-day` / `partly-cloudy-night` |
| 3 | Overcast | `cloudy` |
| 45, 48 | Fog / rime fog | `fog` |
| 51-57, 61-67, 80-82 | Drizzle, rain, showers | `rain` |
| 71-77, 85, 86 | Snow, snow showers | `snow` |
| 95, 96, 99 | Thunderstorm | `thunderstorm` |
| (while fetching) | | `loading` |
| (fetch failed, nothing cached) | | `error` |

Today the app maps 1/2/3 to "partly cloudy" and 45/48 to "rain"; this set makes those distinct.
