param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$PublishRoot = Join-Path $ArtifactsRoot "publish"
$OutputRoot = Join-Path $PublishRoot "self-contained"
$GuiDir = Join-Path $OutputRoot "gui"
$CliDir = Join-Path $OutputRoot "cli"
$RunnerDir = Join-Path $GuiDir "runner"

if (Test-Path $OutputRoot) {
    Remove-Item $OutputRoot -Recurse -Force
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

if (Test-Path $RunnerDir) {
    Remove-Item $RunnerDir -Recurse -Force
}

Publish-Project "Runner" "src/wsl-squad.UI.Runner/wsl-squad.UI.Runner.csproj" $RunnerDir
Publish-Project "CLI" "src/wsl-squad.UI.Cli/wsl-squad.UI.Cli.csproj" $CliDir

Write-Host ""
Write-Host "Published to $OutputRoot"
Write-Host "  GUI    : $(Join-Path $GuiDir 'wsl-squad.gui.exe')"
Write-Host "  Runner : $(Join-Path $RunnerDir 'wsl-squad.UI.Runner.exe')"
Write-Host "  CLI    : $(Join-Path $CliDir 'wsl-squad.cli.exe')"
