#define AppName "WSL Squad"
#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
#define AppPublisher "JustVNRR"
#define GuiExe "wsl-squad.gui.exe"
#define CliExe "wsl-squad.cli.exe"

; 0 = framework-dependent, 1 = self-contained
#define SelfContained 0

; 1 = install redist\windowsdesktop-runtime-10.0.x-win-x64.exe before launching the app
#define BundleRuntime 0

#define RepoRoot ".."
#define ArtifactsDir RepoRoot + "\artifacts"
#define PublishDir ArtifactsDir + "\publish"
#define DistDir ArtifactsDir + "\dist"

[Setup]
AppId={{E7B1F3A2-6C4D-4E8F-9A1B-2D3C4E5F6A7B}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\WSL Squad
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir={#DistDir}
OutputBaseFilename=wsl-squad-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=125
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#GuiExe}
SetupIconFile={#RepoRoot}\src\wsl-squad.UI.Wpf\Assets\app.ico
CloseApplications=yes
RestartApplications=no
ChangesEnvironment=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[LangOptions]
DialogFontSize=12

[Files]
#if SelfContained == 1
Source: "{#PublishDir}\self-contained\gui\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "{#PublishDir}\self-contained\cli\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
#else
Source: "{#PublishDir}\framework-dependent\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
#endif
Source: "wsl-squad.cmd"; DestDir: "{app}"; Flags: ignoreversion

#if BundleRuntime == 1
#if SelfContained == 0
Source: "redist\windowsdesktop-runtime-10.0.x-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
#endif
#endif

[Registry]
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Check: NeedsAddPath(ExpandConstant('{app}'))

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#GuiExe}"
Name: "{group}\{#AppName} CLI"; Filename: "{app}\{#CliExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#GuiExe}"; Tasks: desktopicon

[Run]
#if BundleRuntime == 1
#if SelfContained == 0
Filename: "{tmp}\windowsdesktop-runtime-10.0.x-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing the .NET 10 Desktop Runtime..."; Flags: waituntilterminated
#endif
#endif
Filename: "{app}\{#GuiExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function NeedsAddPath(Param: string): Boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', OrigPath) then
  begin
    Result := True;
    Exit;
  end;

  Result :=
    Pos(
      ';' + Lowercase(Param) + ';',
      ';' + Lowercase(OrigPath) + ';'
    ) = 0;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  PathValue: string;
  AppDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDir := ExpandConstant('{app}');

    if RegQueryStringValue(HKCU, 'Environment', 'Path', PathValue) then
    begin
      StringChangeEx(PathValue, ';' + AppDir, '', True);
      StringChangeEx(PathValue, AppDir + ';', '', True);

      if PathValue = AppDir then
      begin
        PathValue := '';
      end;

      RegWriteExpandStringValue(HKCU, 'Environment', 'Path', PathValue);
    end;
  end;
end;

function WslInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Result :=
    Exec(
      ExpandConstant('{sys}\wsl.exe'),
      '--status',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode
    ) and (ResultCode = 0);

  if not Result then
  begin
    Result :=
      Exec(
        ExpandConstant('{sys}\wsl.exe'),
        '--list',
        '',
        SW_HIDE,
        ewWaitUntilTerminated,
        ResultCode
      ) and (ResultCode = 0);
  end;
end;

function DockerInstalled(): Boolean;
var
  DisplayName: string;
begin
  Result :=
    FileExists(ExpandConstant('{pf}\Docker\Docker\Docker Desktop.exe')) or
    FileExists(ExpandConstant('{localappdata}\Docker\Docker Desktop.exe'));

  if not Result then
  begin
    Result :=
      RegQueryStringValue(
        HKLM,
        'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Docker Desktop',
        'DisplayName',
        DisplayName
      );
  end;

  if not Result then
  begin
    Result :=
      RegQueryStringValue(
        HKCU,
        'Software\Microsoft\Windows\CurrentVersion\Uninstall\Docker Desktop',
        'DisplayName',
        DisplayName
      );
  end;
end;

function HasDesktopRuntime(const Root: string): Boolean;
var
  FindRec: TFindRec;
begin
  Result := FindFirst(Root + '\Microsoft.WindowsDesktop.App\10.*', FindRec);

  if Result then
  begin
    FindClose(FindRec);
  end;
end;

function DotNet10DesktopInstalled(): Boolean;
begin
  Result :=
    HasDesktopRuntime(ExpandConstant('{pf}\dotnet\shared')) or
    HasDesktopRuntime(ExpandConstant('{localappdata}\Microsoft\dotnet\shared'));
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if not WslInstalled() then
  begin
    MsgBox(
      'WSL 2 is required but was not detected.' + #13#10 + #13#10 +
      'Install it with "wsl --install", restart if needed, then run this setup again.' + #13#10 +
      'https://learn.microsoft.com/windows/wsl/install',
      mbError,
      MB_OK
    );
    Result := False;
    Exit;
  end;

#if SelfContained == 0
#if BundleRuntime == 0
  if not DotNet10DesktopInstalled() then
  begin
    MsgBox(
      'The .NET 10 Desktop Runtime is required but was not detected.' + #13#10 + #13#10 +
      'Install it from https://dotnet.microsoft.com/download/dotnet/10.0, then run this setup again.',
      mbError,
      MB_OK
    );
    Result := False;
    Exit;
  end;
#endif
#endif

  if not DockerInstalled() then
  begin
    if MsgBox(
      'Docker Desktop was not found.' + #13#10 + #13#10 +
      'WSL Squad can be installed without it, but Docker builds will not work.' + #13#10 +
      'Continue anyway?',
      mbConfirmation,
      MB_YESNO
    ) = IDNO then
    begin
      Result := False;
      Exit;
    end;
  end;
end;
