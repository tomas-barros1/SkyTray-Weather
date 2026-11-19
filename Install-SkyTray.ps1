# Install-SkyTray.ps1 - Installer script for SkyTray Weather
$ErrorActionPreference = "Stop"

$installDir = "$env:LOCALAPPDATA\SkyTrayWeather"
$repoRoot = $PSScriptRoot
$publishDir = $repoRoot
$packageExecutable = Join-Path $publishDir "WinuiWeatherForecastTray.exe"

# Release ZIP layout: the installer script is next to the already-published app.
if (-not (Test-Path -LiteralPath $packageExecutable -PathType Leaf)) {
    $csproj = Join-Path $repoRoot "WinuiWeatherForecastTray\WinuiWeatherForecastTray.csproj"
    if (-not (Test-Path -LiteralPath $csproj -PathType Leaf)) {
        Write-Error "Neither a published app nor the source project was found beside this script."
    }

    $publishDir = Join-Path $repoRoot "publish"
    Write-Host "Published app not found. Publishing the source project (release, win-x64, self-contained)..." -ForegroundColor Yellow
    dotnet publish $csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to publish the source project (dotnet exit code $LASTEXITCODE)."
    }

    $packageExecutable = Join-Path $publishDir "WinuiWeatherForecastTray.exe"
    if (-not (Test-Path -LiteralPath $packageExecutable -PathType Leaf)) {
        Write-Error "Published app executable was not found: $packageExecutable"
    }
}

Write-Host "Installing SkyTray Weather to $installDir..." -ForegroundColor Cyan

# Stop any running process
# (also the name used before the "Wheater" -> "Weather" rename)
Stop-Process -Name "WinuiWeatherForecastTray", "WinuiWheaterForecastTray" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# Remove leftovers from builds that used the old (misspelled) executable name
Remove-Item -Path "$installDir\WinuiWheaterForecastTray.*" -Force -ErrorAction SilentlyContinue

# Ensure directory exists
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

# Copy files
Copy-Item -Path (Join-Path $publishDir "*") -Destination $installDir -Recurse -Force

$exePath = "$installDir\WinuiWeatherForecastTray.exe"
$icoPath = "$installDir\Assets\app.ico"

# Create Start Menu Shortcut
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$shortcutPath = "$startMenuDir\SkyTray Weather.lnk"

$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = $exePath
$Shortcut.WorkingDirectory = $installDir
$Shortcut.IconLocation = $icoPath
$Shortcut.Description = "SkyTray Weather App"
$Shortcut.Save()

Write-Host "SUCCESS! SkyTray Weather installed to Start Menu!" -ForegroundColor Green
Write-Host "Shortcut path: $shortcutPath" -ForegroundColor Yellow
