; Inno Setup Script for QuickLaunchTool (Lite Version - Requires .NET Runtime)
; Requires Inno Setup 6 or later

#define MyAppName "QuickLaunchTool"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TJF"
#define MyAppExeName "QuickLaunchTool.exe"
#define MyAppURL "https://github.com/TJF/QuickLaunchTool"
#define DotNetVersion "6.0"

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
OutputBaseFilename=QuickLaunchTool-Setup-Lite-{#MyAppVersion}
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
; Main executable (framework-dependent)
Source: "bin\Release\net6.0-windows\*.exe"; DestDir: "{app}"; Flags: ignoreversion
; All DLL files
Source: "bin\Release\net6.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
; Resource folders (language files)
Source: "bin\Release\net6.0-windows\de-DE\*"; DestDir: "{app}\de-DE"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\en-US\*"; DestDir: "{app}\en-US"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\es-ES\*"; DestDir: "{app}\es-ES"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\fr-FR\*"; DestDir: "{app}\fr-FR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\ja-JP\*"; DestDir: "{app}\ja-JP"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\ko-KR\*"; DestDir: "{app}\ko-KR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\zh-CN\*"; DestDir: "{app}\zh-CN"; Flags: ignoreversion recursesubdirs createallsubdirs
; Ico folder
Source: "bin\Release\net6.0-windows\Ico\*"; DestDir: "{app}\Ico"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Launch app after installation (optional)
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if .NET 6.0 Desktop Runtime is installed
function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
begin
  Result := False;

  // Try to run dotnet --list-runtimes to check for .NET 6.0
  if Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    // If dotnet command exists and runs successfully
    Result := True;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  DotNetURL: String;
begin
  Result := True;

  // Check if .NET Runtime is installed
  if not IsDotNetInstalled() then
  begin
    DotNetURL := 'https://dotnet.microsoft.com/download/dotnet/6.0';

    if MsgBox('This application requires .NET {#DotNetVersion} Desktop Runtime to run.' + #13#10#13#10 +
              'Would you like to download it now?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', DotNetURL, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;

    // Allow installation to continue even if user declines download
    // User can install .NET later
    MsgBox('Setup will continue, but you will need to install .NET {#DotNetVersion} Desktop Runtime before running the application.',
           mbInformation, MB_OK);
  end;
end;

[UninstallDelete]
; Clean up any user-created config files (optional, uncomment if needed)
; Type: filesandordirs; Name: "{localappdata}\{#MyAppName}"
