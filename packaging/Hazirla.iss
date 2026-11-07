#define MyAppVersion "0.1.0"
[Setup]
AppId={{C481F6D2-0876-4D5E-85D3-35693E887A19}
AppName=Hazırla
AppVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\Hazirla
DefaultGroupName=Hazırla
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=Hazirla-0.1.0-win-x64-Setup
SetupIconFile=..\Hazirla\Assets\Hazirla.ico
UninstallDisplayIcon={app}\Hazirla.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; Flags: unchecked
[Files]
Source: "..\dist\Hazirla-0.1.0-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Hazırla"; Filename: "{app}\Hazirla.exe"
Name: "{autodesktop}\Hazırla"; Filename: "{app}\Hazirla.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\Hazirla.exe"; Description: "Hazırla’yı aç"; Flags: nowait postinstall skipifsilent
