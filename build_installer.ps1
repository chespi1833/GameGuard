$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "1. Compilando GameGuard (Release)..." -ForegroundColor Cyan
dotnet build "$root\GameGuard\GameGuard.csproj" -c Release

$sourceDir = "$root\GameGuard\bin\Release\net10.0-windows"
$cfgFile = Join-Path $sourceDir "config.json"
if (Test-Path $cfgFile) { Remove-Item $cfgFile -Force }

$payloadZip = "$root\GameGuard.Setup\payload.zip"
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }

Write-Host "2. Empaquetando payload.zip..." -ForegroundColor Cyan
Compress-Archive -Path "$sourceDir\*" -DestinationPath $payloadZip -Force

Write-Host "3. Publicando instalador GameGuard_Setup.exe..." -ForegroundColor Cyan
dotnet publish "$root\GameGuard.Setup\GameGuard.Setup.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$root\Instalador"

Remove-Item $payloadZip -Force -ErrorAction SilentlyContinue
Remove-Item "$root\Instalador\*.pdb" -Force -ErrorAction SilentlyContinue

Write-Host "Listo: $root\Instalador\GameGuard_Setup.exe" -ForegroundColor Green
