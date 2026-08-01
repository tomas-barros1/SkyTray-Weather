# Install-SkyTray.ps1 - Installer script for SkyTray Weather
$ErrorActionPreference = "Stop"

$installDir = "$env:LOCALAPPDATA\SkyTrayWeather"
$repoRoot = $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish"

if (-not (Test-Path $publishDir)) {
    $publishDir = Join-Path $repoRoot "WinuiWheaterForecastTray\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
}

Write-Host "Installing SkyTray Weather to $installDir..." -ForegroundColor Cyan

# Stop any running process
Stop-Process -Name "WinuiWheaterForecastTray" -Force -ErrorAction SilentlyContinue

# Ensure directory exists
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

# Copy files
Copy-Item -Path "$publishDir\*" -Destination $installDir -Recurse -Force

$exePath = "$installDir\WinuiWheaterForecastTray.exe"
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
