param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'outputs'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$sdk = Join-Path $root '.dotnet\dotnet.exe'
if (-not (Test-Path $sdk)) { $sdk = Join-Path $root 'work\.dotnet\dotnet.exe' }
if (-not (Test-Path $sdk)) { throw 'Ejecuta setup.ps1 antes de empaquetar.' }
$temp = Join-Path $root 'work\package'
$publish = Join-Path $temp 'HotSTalentOverlay'
$env:TEMP = Join-Path $root 'work'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_HOME = $env:TEMP
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

if (Test-Path $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
& $sdk restore (Join-Path $root 'src\HotSTalentOverlay.App\HotSTalentOverlay.App.csproj') --runtime win-x64 --source 'https://api.nuget.org/v3/index.json' -p:NuGetAudit=false --disable-build-servers -m:1
if ($LASTEXITCODE -ne 0) { throw "dotnet restore falló con el código $LASTEXITCODE." }
& $sdk publish (Join-Path $root 'src\HotSTalentOverlay.App\HotSTalentOverlay.App.csproj') --configuration Release --runtime win-x64 --self-contained true --output $publish --no-restore -p:NuGetAudit=false --disable-build-servers -m:1
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló con el código $LASTEXITCODE." }

$helperArchive = Join-Path $temp 'HeroesDataParser.5.0.4-scd-win-x64.zip'
$helperDirectory = Join-Path $publish 'tools'
$helperExtract = Join-Path $temp 'helper-extract'
New-Item -ItemType Directory -Force -Path $helperDirectory | Out-Null
if (-not (Test-Path $helperArchive)) {
    Invoke-WebRequest -UseBasicParsing 'https://github.com/HeroesToolChest/HeroesDataParser/releases/download/v5.0.4/HeroesDataParser.5.0.4-scd-win-x64.zip' -OutFile $helperArchive
}
if (Test-Path $helperExtract) { Remove-Item -LiteralPath $helperExtract -Recurse -Force }
Expand-Archive -LiteralPath $helperArchive -DestinationPath $helperExtract -Force
$helperExecutable = Get-ChildItem -LiteralPath $helperExtract -Recurse -Filter 'HeroesDataParser.exe' | Select-Object -First 1
if ($null -eq $helperExecutable) { throw 'El release no contiene HeroesDataParser.exe.' }
Copy-Item -Path (Join-Path $helperExecutable.Directory.FullName '*') -Destination $helperDirectory -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $publish
Copy-Item -LiteralPath (Join-Path $root 'ARCHITECTURE.md') -Destination $publish
Copy-Item -LiteralPath (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination $publish
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $publish
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$zip = Join-Path $OutputDirectory 'HotSTalentOverlay-win-x64.zip'
if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Paquete creado: $zip" -ForegroundColor Green
