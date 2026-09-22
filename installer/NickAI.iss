; NICK AI installer definition (Inno Setup 6)
; Build with: pwsh ./scripts/package-installer.ps1
; The compiler is invoked with /DSourceDir and /DOutputDir overrides.

#define AppName "NICK AI"
#define AppVersion "0.1.0"
#define AppPublisher "Yoga Kumar"
#define AppExeName "NICKAI.exe"
#define AppIdValue "{4F1B0C2A-77D3-4C69-9C1E-5A2F6D3B8E11}"

#ifndef SourceDir
  #define SourceDir "..\publish\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\publish\installer"
#endif

[Setup]
AppId={#AppIdValue}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\NICK AI
DefaultGroupName=NICK AI
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=NICK_AI_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
