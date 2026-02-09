; Inno Setup Script for QuickLaunchTool
; Requires Inno Setup 6 or later

#define MyAppName "QuickLaunchTool"
#define MyAppVersion "v1.0.0.1"
#define MyAppPublisher "TJF"
#define MyAppExeName "QuickLaunchTool.exe"
#define MyAppURL "https://github.com/TJF/QuickLaunchTool"

[Setup]
; Basic app info
AppId={{QuickLaunchTool-GUID-12345678-1234-1234-1234-123456789012}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
; Output settings
OutputDir=installer
OutputBaseFilename=QuickLaunchTool-Setup-{#MyAppVersion}
SetupIconFile=Ico\QuickLaunchTool.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Target OS
MinVersion=10.0.10240
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main executable (self-contained)
Source: "bin\Release\net6.0-windows\win10-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; All other files in the publish directory
Source: "bin\Release\net6.0-windows\win10-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Launch app after installation (optional)
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up any user-created config files (optional, uncomment if needed)
; Type: filesandordirs; Name: "{localappdata}\{#MyAppName}"
