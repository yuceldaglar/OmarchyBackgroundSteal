# Build a single-file WinUI exe for GitHub Releases (win-x64, unpackaged).
# From repo root:  .\release.ps1
# Optional:       .\release.ps1 -Version 0.1.0
#
# The published binary must keep the name OmarchyBackgrounds.App.exe (WinAppSDK
# resources.pri / SxS). We wrap that one file in a versioned zip for Releases.
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

$Version = ($Version -replace '[\\/:\*\?"<>\|]', '-')

$rid = "win-x64"
$project = ".\src\OmarchyBackgrounds.App\OmarchyBackgrounds.App.csproj"
$publishDir = Join-Path $PSScriptRoot "artifacts\publish\$rid"
$appExeName = "OmarchyBackgrounds.App.exe"
$zipName = "OmarchyBackgrounds-$rid-$Version.zip"
$zipPath = Join-Path $PSScriptRoot "artifacts\$zipName"
$stagingDir = Join-Path $PSScriptRoot "artifacts\staging\$rid"

Write-Host "Publishing $rid (self-contained single-file, Release)..."
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

dotnet publish $project `
    -c Release `
    -p:Platform=x64 `
    -r $rid `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:WindowsAppSdkUndockedRegFreeWinRTInitialize=true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishedExe = Join-Path $publishDir $appExeName
if (-not (Test-Path $publishedExe)) {
    throw "Expected executable not found: $publishedExe"
}

$extra = @(Get-ChildItem $publishDir -File | Where-Object { $_.Name -ne $appExeName })
if ($extra.Count -gt 0) {
    Write-Warning ("Publish folder has extra files (expected only the exe): " + (($extra | ForEach-Object Name) -join ", "))
}

# Stage exactly one file under the required exe name, then zip.
if (Test-Path $stagingDir) {
    Remove-Item $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
Copy-Item $publishedExe (Join-Path $stagingDir $appExeName)

New-Item -ItemType Directory -Path (Split-Path $zipPath) -Force | Out-Null
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $stagingDir $appExeName) -DestinationPath $zipPath -CompressionLevel Optimal

$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host ""
Write-Host "Done. Artifact: $zipPath ($sizeMb MB)"
Write-Host "Inside the zip: $appExeName (do not rename — required for WinUI single-file)."
Write-Host "Upload this zip on GitHub -> Releases."
