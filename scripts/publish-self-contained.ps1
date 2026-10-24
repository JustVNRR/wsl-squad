param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$PublishRoot = Join-Path $ArtifactsRoot "publish"
$ModeRoot = Join-Path $PublishRoot "self-contained"
$GuiDir = Join-Path $ModeRoot "gui"
$CliDir = Join-Path $ModeRoot "cli"

if (Test-Path $ModeRoot) {
    Remove-Item $ModeRoot -Recurse -Force
}

$common = @(
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
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

Publish-Project "GUI" "src/wsl-squad.UI.Wpf/wsl-squad.UI.Wpf.csproj" $GuiDir
Publish-Project "CLI" "src/wsl-squad.UI.Cli/wsl-squad.UI.Cli.csproj" $CliDir

Write-Host ""
Write-Host "Published to $ModeRoot"
Write-Host "  GUI    : $(Join-Path $GuiDir 'wsl-squad.gui.exe')"
Write-Host "  CLI    : $(Join-Path $CliDir 'wsl-squad.cli.exe')"
