; Inno Setup Script for SkyTray Weather
#define MyAppName "SkyTray Weather"
#define MyAppPublisher "SkyTray"
#define MyAppURL "https://github.com/tomas-barros1/SkyTray-Weather"
#define MyAppExeName "WinuiWheaterForecastTray.exe"

#ifndef MyAppVersion
#define MyAppVersion "1.1.0"
#endif

[Setup]
AppId={{D37E64B2-9A4B-4E8F-8A0D-3C7E9A1F2B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userlocalappdata}\SkyTrayWeather
DisableProgramGroupPage=yes
OutputBaseFilename=SkyTrayWeather-Setup-x64
SetupIconFile=WinuiWheaterForecastTray\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
