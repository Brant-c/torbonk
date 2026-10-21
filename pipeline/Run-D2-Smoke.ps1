param([switch]$SkipImport)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'D2 C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/d2-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
if (!$SkipImport) {
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/d2-import.log') *> (Join-Path $PSScriptRoot 'logs/d2-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'D2 asset import failed' }
}
$previousAppData = $env:APPDATA
try {
    $env:APPDATA = Join-Path $PSScriptRoot 'work/d2-userdata'
    New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/d2-runtime.log') --max-fps 60 --quit-after 1800 -- --d2-smoke *> (Join-Path $PSScriptRoot 'logs/d2-runtime-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'D2 runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'D2 smoke incomplete or failed' }
if ((Get-Content (Join-Path $PSScriptRoot 'logs/d2-runtime.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'D2 runtime reported errors' }
Write-Host 'D2 passed. Stop for manual gameplay review.'
