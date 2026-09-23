# Build a portable zip for GitHub Releases (self-contained win-x64, unpackaged).
# From repo root:  .\release.ps1
# Optional:       .\release.ps1 -Version 0.1.0
#
# Upload the zip on GitHub → Releases → Draft a new release.
[CmdletBinding()]
param(
    [string] $Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not $Version) {
    $desc = (git describe --tags --always --dirty 2>$null)
    if ($desc) { $Version = $desc.Trim() } else { $Version = "0.0.0-dev" }
}

# Zip-safe: avoid path separators from git describe
$Version = ($Version -replace '[\\/:\*\?"<>\|]', '-')

$rid = "win-x64"
$project = ".\src\OmarchyBackgrounds.App\OmarchyBackgrounds.App.csproj"
$publishDir = Join-Path $PSScriptRoot "artifacts\publish\$rid"
$zipName = "OmarchyBackgrounds-$rid-$Version.zip"
$zipPath = Join-Path $PSScriptRoot "artifacts\$zipName"

Write-Host "Publishing $rid (self-contained, Release)..."
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# PublishTrimmed=false: WinUI + Windows App SDK are safer untrimmed for portable runs.
dotnet publish $project `
    -c Release `
    -p:Platform=x64 `
    -r $rid `
    --self-contained true `
    -p:PublishTrimmed=false `
    -p:PublishSingleFile=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $publishDir "OmarchyBackgrounds.App.exe"
if (-not (Test-Path $exe)) {
    throw "Expected executable not found: $exe"
}

Write-Host "Zipping -> $zipPath"
New-Item -ItemType Directory -Path (Split-Path $zipPath) -Force | Out-Null
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host ""
Write-Host "Done. Artifact: $zipPath ($sizeMb MB)"
Write-Host "Run after unzip: OmarchyBackgrounds.App.exe"
Write-Host "Upload this zip on GitHub -> Releases."
