<#
.SYNOPSIS
Runs the Windows integration suite against WSL and Docker Desktop.

.DESCRIPTION
Requires Windows 10/11 with WSL2 and Docker Desktop installed and running.
The suite reads the real WSL registry, lists running distributions and checks
Docker availability. It does not create or delete distributions; temporary
configuration roots are isolated under %TEMP% and removed afterwards.

.PARAMETER NoBuild
Skips the Release build and runs the tests against existing binaries.
#>
[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$runId = [guid]::NewGuid().ToString('N')
$env:WSL_SQUAD_CONFIG_DIR = Join-Path $env:TEMP "wsl-squad-it-config-$runId"
$env:WSL_SQUAD_ROOT = Join-Path $env:TEMP "wsl-squad-it-fleet-$runId"

try {
    if (-not $NoBuild) {
        dotnet build wsl-squad.slnx -c Release
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }

    dotnet test tests/wsl-squad.Windows.Tests/wsl-squad.Windows.Tests.csproj `
        -c Release --no-build --filter "Suite=WindowsIntegration"
    if ($LASTEXITCODE -ne 0) { throw 'Windows integration tests failed.' }

    Write-Host 'Windows integration suite passed.' -ForegroundColor Green
}
finally {
    Pop-Location
    Remove-Item -Recurse -Force $env:WSL_SQUAD_CONFIG_DIR -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $env:WSL_SQUAD_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:WSL_SQUAD_CONFIG_DIR -ErrorAction SilentlyContinue
    Remove-Item Env:WSL_SQUAD_ROOT -ErrorAction SilentlyContinue
}
