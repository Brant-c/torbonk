param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B6 C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/b6-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/b6-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/b6-import.log') *> (Join-Path $PSScriptRoot 'logs/b6-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'B6 local import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/b6-runtime.log') --max-fps 60 --quit-after 2400 -- --b6-smoke
    if ($LASTEXITCODE -ne 0) { throw 'B6 runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B6 smoke incomplete or failed' }
if ((Get-Content (Join-Path $PSScriptRoot 'logs/b6-runtime.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'B6 runtime reported errors' }
Write-Host 'B6 smoke passed. Ready for manual gameplay/visual review.'
