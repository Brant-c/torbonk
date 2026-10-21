param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Storm C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/storm-source-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/storm-source-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/storm-source-import.log') *> (Join-Path $PSScriptRoot 'logs/storm-source-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'Storm changed-asset import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/storm-source-runtime.log') --max-fps 60 --quit-after 1200 -- --storm-source-smoke
    if ($LASTEXITCODE -ne 0) { throw 'Storm runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'Storm smoke incomplete or failed' }
if ((Get-Content (Join-Path $PSScriptRoot 'logs/storm-source-runtime.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'Storm runtime reported errors' }
Write-Host 'Original Storm smoke passed. Stop for manual visual review.'
