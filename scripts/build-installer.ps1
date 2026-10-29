# Publishes the framework-dependent build, then builds the Inno Setup installer.
# Default version: 0.1.0.
# Usage:
#   .\scripts\build-installer.ps1
#   .\scripts\build-installer.ps1 -Version 0.1.0
#   .\scripts\build-installer.ps1 -Version 0.2.0-beta.1
#   powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -Version 0.1.0

param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$Version = $Version.Trim()

if ($Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $Version = $Version.Substring(1)
}

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    throw "Invalid version '$Version'. Expected a value such as 0.1.0 or 0.1.0-beta.1."
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$InstallerScript = Join-Path $RepoRoot "installer\wsl-squad.iss"
$PublishScript = Join-Path $PSScriptRoot "publish-framework-dependent.ps1"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\framework-dependent"

if (-not (Test-Path $InstallerScript)) {
    throw "Installer script not found: $InstallerScript"
}

if (-not (Test-Path $PublishScript)) {
    throw "Publish script not found: $PublishScript"
}

$Iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source

if (-not $Iscc -and $env:LOCALAPPDATA) {
    $PerUser = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"

    if (Test-Path $PerUser) {
        $Iscc = $PerUser
    }
}

if (-not $Iscc) {
    $MachineRoot = ${env:ProgramFiles(x86)}

    if (-not $MachineRoot) {
        $MachineRoot = $env:ProgramFiles
    }

    if ($MachineRoot) {
        $Machine = Join-Path $MachineRoot "Inno Setup 6\ISCC.exe"

        if (Test-Path $Machine) {
            $Iscc = $Machine
        }
    }
}

if (-not $Iscc) {
    throw "ISCC.exe not found. Install Inno Setup 6 or add it to PATH."
}

Write-Host "Publishing WSL Squad $Version..."
& $PublishScript -Version $Version

if (-not (Test-Path $PublishDir)) {
    throw "Published files not found: $PublishDir."
}

Write-Host "Building WSL Squad $Version..."
& $Iscc $InstallerScript "/DAppVersion=$Version"

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$Setup = Join-Path $RepoRoot "artifacts\dist\wsl-squad-setup.exe"
Write-Host "Installer: $Setup"
