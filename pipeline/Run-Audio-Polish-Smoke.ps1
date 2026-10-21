param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Audio polish build failed' }
$previousAppData = $env:APPDATA
$receipt = Join-Path $PSScriptRoot 'manifests/audio-polish-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
try {
    $env:APPDATA = Join-Path $PSScriptRoot 'work/b7-userdata'
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/audio-polish-import.log') *> (Join-Path $PSScriptRoot 'logs/audio-polish-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'Audio import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/audio-polish-runtime.log') --max-fps 60 --quit-after 1800 -- --audio-polish-smoke
    if ($LASTEXITCODE -ne 0) { throw 'Audio polish smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'Audio smoke incomplete' }
if ((Get-Content (Join-Path $PSScriptRoot 'logs/audio-polish-runtime.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'Audio runtime reported errors' }
Write-Host 'Audio polish smoke passed. Ready for listening review.'
