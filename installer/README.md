# Windows installer

This folder contains the WSL Squad Inno Setup script.

```text
installer\
├── wsl-squad.iss
├── redist\
│   └── windowsdesktop-runtime-10.0.x-win-x64.exe
└── README.md
```

## Prerequisites

- Inno Setup 6 installed on the build machine. The installer does not add `iscc` to `PATH` by default; the executable is located according to the installation mode:
  - machine-wide install: `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`;
  - per-user install, typically with `winget` without elevation: `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`.
- Publication outputs present in `artifacts\publish\`:

```powershell
.\scripts\publish-framework-dependent.ps1
```

or:

```powershell
.\scripts\publish-self-contained.ps1
```

## Build

From the repository root:

```powershell
iscc installer\wsl-squad.iss
```

If `iscc` is not in `PATH`, check whether it can be found:

```powershell
Get-Command iscc -ErrorAction SilentlyContinue
```

Then use the full path matching your installation:

```powershell
# Machine-wide install
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\wsl-squad.iss

# Per-user install
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\wsl-squad.iss
```

Output:

```text
artifacts\dist\wsl-squad-setup.exe
```

`artifacts\` groups all generated outputs: `bin\`, `obj\`, `publish\` and `dist\`. The folder is ignored by Git. The `.iss` script locates it with `RepoRoot + "\artifacts"` from the `installer\` folder.

## Wizard size

The setup window uses `WizardSizePercent=125` and the dialog font uses `DialogFontSize=12`. Adjust these values in `wsl-squad.iss` if the wizard is too large or too small. `WizardSizePercent` changes the window size; `DialogFontSize` changes the text size.

## Publication modes

The script defaults to a **framework-dependent** publication:

```ini
#define SelfContained 0
```

For a self-contained installer:

```ini
#define SelfContained 1
```

In this mode:

- the .NET runtime is not required;
- the Desktop Runtime check is disabled;
- files come from `artifacts\publish\self-contained\gui\*` and `artifacts\publish\self-contained\cli\*`; the CLI is installed under `cli\`.

## Bundled .NET runtime

For a framework-dependent publication, you can:

1. leave `BundleRuntime` at `0` and require the user to install the .NET 10 Desktop Runtime;
2. set `BundleRuntime` to `1` and place the runtime installer in:

```text
installer\redist\windowsdesktop-runtime-10.0.x-win-x64.exe
```

The setup runs it with:

```text
/install /quiet /norestart
```

The runtime installer may request UAC elevation. The WSL Squad installation itself remains per-user.

## Setup checks

The script checks:

- **WSL** with `wsl.exe --status`, then `wsl.exe --list` as a fallback. WSL is required; installation is blocked if it is missing.
- **Docker Desktop** through the `Program Files` path or the registry. Docker is optional: a warning allows the user to continue.
- **.NET 10 Desktop Runtime** in framework-dependent mode, unless `BundleRuntime` is enabled.

## `wsl-squad` command

The installer copies `wsl-squad.cmd` into the installation folder and adds that folder to the user `PATH`.

After installation, in a **new** terminal:

```powershell
wsl-squad
```

Without arguments, the command opens the CLI interactive menu. There is no `--help` option.

The `.cmd` launches `cli\wsl-squad.cli.exe` and propagates its exit code.

No `wsl-squad` launcher is created for the GUI; since the installation folder is on `PATH`, `wsl-squad.gui` can still be invoked, but the GUI normally starts from its shortcut.

The uninstaller removes the folder from `PATH`.

## Installer test procedure

Run this on a Windows machine, ideally without the .NET SDK for the framework-dependent mode.

### 1. Build

From the repository root:

```powershell
iscc installer\wsl-squad.iss
```

Verify that `artifacts\dist\wsl-squad-setup.exe` exists.

### 2. Install

Run `artifacts\dist\wsl-squad-setup.exe`.

- Verify the WSL check and the Docker warning.
- In framework-dependent mode, verify the .NET 10 Desktop Runtime check.
- Verify the installation completes without UAC elevation, unless `BundleRuntime=1`.
- Verify the `%LOCALAPPDATA%\Programs\WSL Squad` folder.

### 3. Test in a new terminal

Open a **new** terminal so that `PATH` is reloaded:

```powershell
Get-Command wsl-squad
wsl-squad
```

`Get-Command` must return `wsl-squad.cmd` in `%LOCALAPPDATA%\Programs\WSL Squad`. `wsl-squad` must display the CLI menu; quit with the `exit` entry.

Optional test of the wrapper and exit-code propagation:

```powershell
wsl-squad unknown_command
$LASTEXITCODE   # must be 1
```

### 4. Verify PATH

```powershell
([Environment]::GetEnvironmentVariable('Path', 'User') -split ';') |
  Where-Object { $_ -match 'WSL Squad' }
```

The folder must appear **only once**.

### 5. Verify shortcuts and the GUI

- The Start menu shortcut launches `wsl-squad.gui.exe`.
- The desktop shortcut appears only if the corresponding task was selected.
- The application icon is visible in Explorer.
- `cli\wsl-squad.cli.exe` is present in the installation folder.

### 6. Uninstall

Settings → Apps → WSL Squad → Uninstall, or run `unins000.exe` from the installation folder.

### 7. Verify after uninstall

Open a **new** terminal:

```powershell
Get-Command wsl-squad -ErrorAction SilentlyContinue
([Environment]::GetEnvironmentVariable('Path', 'User') -split ';') |
  Where-Object { $_ -match 'WSL Squad' }
```

Both commands must return nothing. Verify that `%LOCALAPPDATA%\wsl-squad` still exists: the uninstaller does not delete user data.

### 8. Verify an upgrade

Reinstall the setup over the existing installation:

- files are replaced;
- `PATH` still contains a single entry;
- `%LOCALAPPDATA%\wsl-squad` is preserved.

## Version

The default version is defined in `wsl-squad.iss`:

```ini
#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
```

The release workflow passes the version with `/DAppVersion=<version>`. When `AppVersion` is not defined on the command line, the default above is used.

Keep the same `AppId` between versions so that upgrades replace the existing installation.

## User data

The application stores its data in:

```text
%LOCALAPPDATA%\wsl-squad
```

The uninstaller does not delete this folder. It contains the application's packs, recipes, themes, fonts and caches.

## Signing

Sign the setup before distribution:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a `
  artifacts\dist\wsl-squad-setup.exe
```

## Ignored files

- `artifacts\`: all generated outputs (`bin`, `obj`, `publish`, `dist`);
- `installer\redist\*.exe`: redistributable runtime, not versioned.

The `.iss` and README files are versioned.
