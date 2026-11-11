; Inno Setup script for ePad. Built by build\release.ps1, which passes the
; version and the folder holding the published ePad.exe:
;   ISCC.exe /DAppVersion=1.0.0 /DSourceDir=<publish folder> ePad.iss

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\publish\win-x64"
#endif

[Setup]
; Never change AppId: Windows uses it to recognise upgrades and the uninstaller.
AppId={{C291862A-6BBC-49F7-B996-0F1301B00B50}
AppName=ePad
AppVersion={#AppVersion}
AppVerName=ePad {#AppVersion}
AppPublisher=Gnomic Software
AppCopyright=Copyright © 2026 Gnomic Software
VersionInfoVersion={#AppVersion}
VersionInfoCompany=Gnomic Software
VersionInfoDescription=ePad Setup

; Installs per user without admin rights by default; the user can choose
; "install for all users" (which asks for admin) on the first page.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\Gnomic Software\ePad
DisableProgramGroupPage=yes

; 64-bit build. "x64compatible" also allows Windows 11 on ARM, which runs x64 apps.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

; Closes a running ePad before upgrading or uninstalling.
CloseApplications=yes

OutputDir=..\artifacts
OutputBaseFilename=ePad-Setup-{#AppVersion}
SetupIconFile=..\src\ePad\ePad.ico
UninstallDisplayIcon={app}\ePad.exe
UninstallDisplayName=ePad
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes

[Languages]
; Same languages as the app.
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french";  MessagesFile: "compiler:Languages\French.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\ePad.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\ePad"; Filename: "{app}\ePad.exe"
Name: "{autodesktop}\ePad"; Filename: "{app}\ePad.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ePad.exe"; Description: "{cm:LaunchProgram,ePad}"; Flags: nowait postinstall skipifsilent

; Notes live in %LOCALAPPDATA%\ePad\notes.db and are deliberately NOT removed
; on uninstall, so reinstalling or upgrading never loses anyone's notes.
