# Install-SkyTray.ps1 - Installer script for SkyTray Weather
$ErrorActionPreference = "Stop"

$installDir = "$env:LOCALAPPDATA\SkyTrayWeather"
$repoRoot = $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish"
$csproj = Join-Path $repoRoot "WinuiWeatherForecastTray\WinuiWeatherForecastTray.csproj"

# Se não houver pasta publish pronta, publica o projeto primeiro (release, win-x64, self-contained).
if (-not (Test-Path $publishDir)) {
    Write-Host "Pasta publish não encontrada. Publicando o projeto (isso pode demorar)..." -ForegroundColor Yellow
    $publishOutput = dotnet publish $csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o $publishDir 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao publicar o projeto. Veja o erro do dotnet acima."
    }
    Write-Host $publishOutput
}

if (-not (Test-Path $publishDir)) {
    Write-Error "Não foi possível localizar a pasta de publicação: $publishDir"
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
Copy-Item -Path "$publishDir\*" -Destination $installDir -Recurse -Force

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
