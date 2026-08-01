# Master Task List — SkyTray Weather Code Review Fixes

Source: panoramic code review of `WinuiWheaterForecastTray.Core` (services + models) and surrounding code.
Target: agents/developers fixing the findings in waves.

## Priority order

The order below is what the review recommends. **Lead with what matters — correctness and security first, then structural regressions and missed simplifications, then cosmetic polish.**

1. **`tasks/01-correctness-fixes.md`** — Required (no-prefix). Blocks production behavior. Includes the `IpLocationService` wrong fallback URL, lat/lon zero-check, i18n `pt-PT` bug, mutable model state, null dereferences in `LocationService`, silent failure in `WeatherService` orchestration.
2. **`tasks/02-performance-fixes.md`** — Required. Sequential awaits in `WeatherService`, double call to `WeatherHelper`, two passes over hourly data, allocation of `CultureInfo` per call.
3. **`tasks/03-architecture-fixes.md`** — Required structural. WMO mapping duplication, two services behind one interface, exception-swallowing contract.
4. **`tasks/04-readability-fixes.md`** — Required polish. Magic strings duplicated between model and service, hardcoded URLs, missing XML docs, inconsistent `sealed` modifiers.
5. **`tasks/05-dead-code-hygiene.md`** — Optional. Hardcoded fallback strings, unused using statements, ambiguous legacy registry keys.

## How to use these files

Each file is **self-contained**: it lists findings with severity, file:line references, and a recommended remedy. Hand the file to a fix agent (or read it yourself) and address every item with severity `Required` before moving to the next file.

### Severity convention

| Prefix | Meaning | Action |
|--------|---------|--------|
| **Critical:** | Blocks merge; security or data loss | Fix first, no exceptions |
| *(no prefix)* | Required change | Must address before merge |
| **Optional:** / **Consider:** | Suggestion | Worth considering but not required |
| **Nit:** | Minor, optional | Author may ignore |
| **FYI** | Informational only | No action needed |

## Verification story (apply after every wave)

Run from repo root:

```powershell
dotnet restore WinuiWheaterForecastTray.slnx
dotnet test  WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release
dotnet build WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -p:Platform=x64
```

After the full set is merged, manually run the WinUI app once and exercise:
- Tray icon render on a real weather code change
- Settings window open/save
- Autostart toggle (verify HKCU write)
- Network failure path (offline mode → does the app degrade gracefully?)
- `pt-PT` system locale (or temporary override via `I18nService(localeOverride: "pt-PT")`)

## Out of scope

These are explicitly **not** part of any wave:
- Migrating to `Microsoft.Extensions.DependencyInjection` (separate decision; tracked separately)
- Migrating to `IHttpClientFactory` (separate decision)
- Adopting `ILogger<T>` (separate decision)
- WinUI 3 / XAML review (the panoramic review only covered Core; UI files are out of scope until a separate review)
- Bumping dependencies (`Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools`, `Microsoft.NET.Test.Sdk`, xUnit, Moq, FluentAssertions) — see AGENTS.md Dependency Discipline; do these one at a time with changelog review.

## Files referenced

| File | Purpose |
|---|---|
| `tasks/00-master-tasks.md` | This file |
| `tasks/01-correctness-fixes.md` | Correctness and security findings |
| `tasks/02-performance-fixes.md` | Performance wins |
| `tasks/03-architecture-fixes.md` | Structural remedies |
| `tasks/04-readability-fixes.md` | Polish and consistency |
| `tasks/05-dead-code-hygiene.md` | Dead code + minor cleanups |
