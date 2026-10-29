param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$Version = $Version.Trim()

if ($Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $Version = $Version.Substring(1)
}

if ($Version -and $Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    throw "Invalid version '$Version'. Expected a value such as 0.1.0 or 0.1.0-beta.1."
}

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

if ($Version) {
    $common += @("-p:Version=$Version")
}

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
