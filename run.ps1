# Run the WinUI app (packaged via Windows App SDK run support).
# From repo root:  .\run.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
dotnet run --project .\src\OmarchyBackgrounds.App\OmarchyBackgrounds.App.csproj -c Debug -p:Platform=x64
