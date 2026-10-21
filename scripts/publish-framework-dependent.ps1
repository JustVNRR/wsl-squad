param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$PublishRoot = Join-Path $ArtifactsRoot "publish"
$AppDir = Join-Path $PublishRoot "framework-dependent"
$CliDir = Join-Path $AppDir "cli"

if (Test-Path $AppDir) {
    Remove-Item $AppDir -Recurse -Force
}

$common = @(
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "false",
    "-p:PublishSingleFile=true",
    "-p:ValidateExecutableReferencesMatchSelfContained=false",
    "-p:DebugSymbols=false",
    "-p:DebugType=None",
    "-p:PublishDocumentationFiles=false",
    "-p:PublishReferencesDocumentationFiles=false",
    "-p:PublishReferencesSymbols=false"
)

function Publish-Project {
    param(
        [string]$Name,
        [string]$Project,
        [string]$Output
    )

    Write-Host "Publishing $Name..."
    $arguments = @("publish", (Join-Path $RepoRoot $Project)) + $common + @("-o", $Output)
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name publish failed."
    }
}

Publish-Project "GUI" "src/wsl-squad.UI.Wpf/wsl-squad.UI.Wpf.csproj" $AppDir

if (Test-Path $CliDir) {
    Remove-Item $CliDir -Recurse -Force
}

Publish-Project "CLI" "src/wsl-squad.UI.Cli/wsl-squad.UI.Cli.csproj" $CliDir

Write-Host ""
Write-Host "Published to $AppDir"
Write-Host "  GUI    : $(Join-Path $AppDir 'wsl-squad.gui.exe')"
Write-Host "  CLI    : $(Join-Path $CliDir 'wsl-squad.cli.exe')"
Write-Host "Requires the .NET 10 Desktop Runtime."
