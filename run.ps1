$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$env:DOTNET_ROOT = Join-Path $root '.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$app = Join-Path $root 'dist\HotSTalentOverlay.dll'
if (-not (Test-Path $app)) { throw 'La app no está compilada. Ejecuta primero .\setup.ps1' }
& (Join-Path $root '.dotnet\dotnet.exe') $app

