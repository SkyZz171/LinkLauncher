; Script Inno Setup de LinkLauncher.
; Compilation : installer\build.ps1 -Installer
; Le paquet publié doit se trouver dans artifacts\publish (voir build.ps1).

#define AppName "LinkLauncher"
; build.ps1 transmet la version avec /DAppVersion=<version> ; cette valeur ne sert que par defaut.
#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif
#define AppPublisher "LinkLauncher"
#define AppExeName "LinkLauncher.exe"

[Setup]
AppId={{9E1B6B2E-3C4B-4C55-9D0A-6C6B7A1F0D31}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=LinkLauncher-{#AppVersion}-setup
SetupIconFile=..\src\LinkLauncher.App\Assets\linklauncher.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
MinVersion=10.0.17763

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Creer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Lancer {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
{ LinkLauncher refuse de demarrer sur une version de Windows anterieure a Windows 10 1809. }
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  Result := (Version.Major > 10) or ((Version.Major = 10) and (Version.Build >= 17763));
  if not Result then
    MsgBox('LinkLauncher necessite Windows 10 version 1809 ou ulterieure.', mbError, MB_OK);
end;
