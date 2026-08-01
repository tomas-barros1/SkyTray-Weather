# 05 — Dead-Code Hygiene

Optional cleanup. After the structural changes in waves 01–04, some artifacts become unused. **Don't delete anything you're unsure about — list it and ask.**
**Land last.**

---

## D-01 [Optional] Legacy `WinuiWheaterForecastTray` registry key

**File:** `WinuiWheaterForecastTray.Core/Services/AutostartService.cs:~45-55`
**Severity:** Optional (cleanup)

The `LegacyAppName = "WinuiWheaterForecastTray"` constant exists to migrate old registry entries from a previous app name. After one or two release cycles the migration code is dead.

**Remedy:** Confirm via `reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v WinuiWheaterForecastTray` on the maintainer's machine. If no entries exist, remove the migration block and the constant. Keep the constant as a `// TODO:` for one release, then delete.

---

## D-02 [Optional] Hardcoded fallback strings after i18n migration

**Files:**
- `WinuiWheaterForecastTray.Core/Models/CurrentWeatherInfo.cs` (after R-01)
- `WinuiWheaterForecastTray.Core/Services/WeatherService.cs` (after R-01)

After R-01 moves defaults to `WeatherService`, the model-level defaults become dead.

**Remedy:** Remove the dead initializers. Replace with `string.Empty` or `0` defaults. The orchestrator is the only place that sets real values today; the defaults were only used when the API returned no data (which should now throw per C-05).

---

## D-03 [Optional] `using` directives that became unused after edits

**Files:** every file touched by waves 01–04
**Severity:** Optional (cleanup)

Refactors (especially A-03 splitting `WeatherService` and R-04/R-11 adding XML docs) often leave `using System.Diagnostics;`, `using System.Globalization;`, etc., unused in some files.

**Remedy:** Run `dotnet format WinuiWheaterForecastTray.Core/WinuiWheaterForecastTray.Core.csproj` after each wave. Commit the result as a single cleanup commit per wave (so the diff stays reviewable).

---

## D-04 [Optional] Old TODO/FIXME markers

**Files:** services and models
**Severity:** Optional (cleanup)

Search the codebase for `// TODO`, `// FIXME`, `// HACK` before and after each wave. Markers referencing resolved issues should be removed; markers that are now actionable should be promoted to issues.

**Remedy:** Run `grep -rn "TODO\|FIXME\|HACK" WinuiWheaterForecastTray.Core` (or `rg` if available). For each hit:
- If the issue is fixed, delete the comment.
- If the issue is still open, decide: do it now (add a task here), or file a GitHub issue and remove the comment.

---

## D-05 [Optional] Old `bin/`, `obj/` directories

**Files:** in every project folder
**Severity:** Optional (already in `.gitignore`)

After the project migrated between IDEs or .NET SDK versions, stale `obj/` directories may contain `AssemblyInfo.cs` files that don't reflect the current csproj. They're ignored by git but can confuse IDE searches.

**Remedy:** Don't delete `obj/` manually — let `dotnet clean` handle it:

```powershell
dotnet clean WinuiWheaterForecastTray.Core/WinuiWheaterForecastTray.Core.csproj
dotnet clean WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj
dotnet clean WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj
```

If something in `obj/` is being read by another tool, fix that tool's reading path rather than hand-editing `obj/`.

---

## D-06 [Optional] `.vs/` artifacts

**Files:** `.vs/` directory at root
**Severity:** Optional

`.vs/` is in `.gitignore` but may have leftover Visual Studio state that causes confusing IDE behavior.

**Remedy:** Safe to `Remove-Item -Recurse .vs` if you're not currently inside a VS session. Reopen VS to regenerate.

---

## D-07 [Optional] `*.user`, `*.suo`

**Files:** `WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj.user` and similar
**Severity:** Optional

Already in `.gitignore`. Check `git status` to confirm none are tracked.

**Remedy:** If any are tracked, `git rm --cached <file>` (do not delete the local file — it's your IDE state).

---

## Verification after this wave

```powershell
git status
```

The output should show only intended files changed by waves 01–04 plus the new task files in `tasks/`. No stray build artifacts, no `*.user` files, no temp files.

```powershell
dotnet restore WinuiWheaterForecastTray.slnx
dotnet test  WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --configuration Release
dotnet build WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -p:Platform=x64
```

All green. Then:

```powershell
git diff --stat
```

The diff stat should be reviewable. If a single wave produced >300 lines of changes, consider splitting it before merge (per AGENTS.md change-sizing rules).
