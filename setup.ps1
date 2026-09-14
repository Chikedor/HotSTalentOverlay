param([switch]$SkipPublish)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$sdk = Join-Path $root '.dotnet\dotnet.exe'
$temp = Join-Path $root 'work'
New-Item -ItemType Directory -Force -Path $temp | Out-Null
$env:TEMP = $temp
$env:TMP = $temp
$env:DOTNET_CLI_HOME = $temp
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

if (-not (Test-Path $sdk)) {
    $installer = Join-Path $temp 'dotnet-install.ps1'
    Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & $installer -Channel 10.0 -InstallDir (Join-Path $root '.dotnet') -NoPath
}

$env:DOTNET_ROOT = Join-Path $root '.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$helper = Join-Path $root '.tools\dotnet-heroes-data-parser.exe'
if (-not (Test-Path $helper)) {
    & $sdk tool install HeroesDataParser --tool-path (Join-Path $root '.tools') --version 5.0.4 --add-source 'https://api.nuget.org/v3/index.json'
}

& $sdk restore (Join-Path $root 'HotSTalentOverlay.slnx') --source 'https://api.nuget.org/v3/index.json' --disable-parallel --disable-build-servers
& $sdk test (Join-Path $root 'HotSTalentOverlay.slnx') --no-restore --configuration Release --disable-build-servers -m:1
if (-not $SkipPublish) {
    & $sdk publish (Join-Path $root 'src\HotSTalentOverlay.App\HotSTalentOverlay.App.csproj') --no-restore --configuration Release --output (Join-Path $root 'dist')
}
Write-Host 'Listo. Ejecuta .\run.ps1 y abre http://127.0.0.1:3874/' -ForegroundColor Green
