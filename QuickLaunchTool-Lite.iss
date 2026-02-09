; Inno Setup Script for QuickLaunchTool (Lite Version - Requires .NET Runtime)
; Requires Inno Setup 6 or later

#define MyAppName "QuickLaunchTool"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TJF"
#define MyAppExeName "QuickLaunchTool.exe"
#define MyAppURL "https://github.com/TJF/QuickLaunchTool"
#define DotNetVersion "6.0"
#define DotNetDownloadURL "https://dotnet.microsoft.com/en-us/download/dotnet/6.0"

[Setup]
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
OutputDir=installer
OutputBaseFilename=QuickLaunchTool-Setup-Lite-{#MyAppVersion}
SetupIconFile=Ico\QuickLaunchTool.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0.10240
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net6.0-windows\*.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\de-DE\*"; DestDir: "{app}\de-DE"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\en-US\*"; DestDir: "{app}\en-US"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\es-ES\*"; DestDir: "{app}\es-ES"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\fr-FR\*"; DestDir: "{app}\fr-FR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\ja-JP\*"; DestDir: "{app}\ja-JP"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\ko-KR\*"; DestDir: "{app}\ko-KR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\zh-CN\*"; DestDir: "{app}\zh-CN"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\Ico\*"; DestDir: "{app}\Ico"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]

//=========================================================================
// 检测 .NET 6.0 Desktop Runtime 是否已安装
// 方法: 检查标准安装目录下是否存在 6.0.* 版本文件夹
//=========================================================================
function IsDotNet6DesktopRuntimeInstalled(): Boolean;
var
  FindRec: TFindRec;
  RuntimePath: String;
begin
  Result := False;

  // ---- 方法1: 检查文件系统 (最可靠) ----
  // .NET Desktop Runtime 标准安装路径:
  // C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\6.0.x
  RuntimePath := ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App');

  if FindFirst(RuntimePath + '\6.0.*', FindRec) then
  begin
    try
      repeat
        // 确保匹配到的是目录而非文件
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Log('.NET 6.0 Desktop Runtime found: ' + FindRec.Name);
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;

  // ---- 方法2: 备用 - 检查注册表 ----
  // 某些精简安装可能路径不同，通过注册表二次确认
  if not Result then
  begin
    if RegValueExists(HKEY_LOCAL_MACHINE,
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
      '6.0') then
    begin
      Log('.NET 6.0 Desktop Runtime found via registry (major version key).');
      Result := True;
    end;
  end;

  if not Result then
    Log('.NET 6.0 Desktop Runtime NOT detected.');
end;

//=========================================================================
// 安装初始化 - 带 Retry 循环的 .NET 运行时检测
//=========================================================================
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  MsgResult: Integer;
  BrowserOpened: Boolean;
begin
  Result := True;
  BrowserOpened := False;

  // ===== 循环检测，直到用户安装成功或主动取消 =====
  while not IsDotNet6DesktopRuntimeInstalled() do
  begin

    if not BrowserOpened then
    begin
      //---------------------------------------------------------------
      // 首次提示: 告知用户缺少运行时，询问是否打开下载页面
      //---------------------------------------------------------------
      MsgResult := MsgBox(
        'This application requires .NET {#DotNetVersion} Desktop Runtime to run, '
        + 'but it was not detected on your system.' + #13#10
        + #13#10
        + 'Click [OK] to open the official download page.' + #13#10
        + 'Click [Cancel] to exit setup.',
        mbCriticalError, MB_OKCANCEL);

      if MsgResult = IDCANCEL then
      begin
        Log('User cancelled setup at .NET download prompt.');
        Result := False;
        Exit;
      end;

      // 打开 .NET 6.0 官方下载页面
      ShellExec('open', '{#DotNetDownloadURL}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      BrowserOpened := True;
      Log('Opened .NET download page in browser.');
    end;

    //---------------------------------------------------------------
    // Retry 循环: 等待用户下载安装完成后点击"重试"
    //---------------------------------------------------------------
    MsgResult := MsgBox(
      'Please download and install ".NET {#DotNetVersion} Desktop Runtime (x64)" '
      + 'from the opened webpage.' + #13#10
      + #13#10
      + 'Download link:' + #13#10
      + '{#DotNetDownloadURL}' + #13#10
      + #13#10
      + '  [Retry]  - I have finished installing, check again' + #13#10
      + '  [Cancel] - Exit setup',
      mbInformation, MB_RETRYCANCEL);

    if MsgResult = IDCANCEL then
    begin
      Log('User cancelled setup at .NET retry prompt.');
      Result := False;
      Exit;
    end;

    // 用户点击了 Retry → 循环回到 while 条件重新检测
    Log('User clicked Retry, re-checking .NET runtime...');
  end;

  // 检测通过
  Log('.NET 6.0 Desktop Runtime check passed. Proceeding with setup.');
end;

[UninstallDelete]
; Type: filesandordirs; Name: "{localappdata}\{#MyAppName}"
