param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B7 C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/b7-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/b7-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/b7-import.log') *> (Join-Path $PSScriptRoot 'logs/b7-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'B7 changed-asset import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/b7-runtime.log') --max-fps 60 --quit-after 2400 -- --b7-smoke
    if ($LASTEXITCODE -ne 0) { throw 'B7 runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B7 smoke incomplete or failed' }
if ((Get-Content (Join-Path $PSScriptRoot 'logs/b7-runtime.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'B7 runtime reported errors' }
Write-Host 'B7 smoke passed. Stop here for manual visual and audio review.'
