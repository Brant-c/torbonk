param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Storm reticle C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/storm-reticle-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$runtimeLog = Join-Path $PSScriptRoot 'logs/storm-reticle-smoke-runtime.log'
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/storm-reticle-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    # Import only new local assets before the single gameplay smoke.
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/storm-reticle-import.log')
    if ($LASTEXITCODE -ne 0) { throw 'Storm reticle asset import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file $runtimeLog --quit-after 900 -- --storm-reticle-smoke
    if ($LASTEXITCODE -ne 0) { throw 'Storm reticle runtime smoke failed; see log' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'Storm reticle smoke incomplete or failed' }
if ((Get-Content -LiteralPath $runtimeLog -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'Storm reticle runtime reported errors; see log' }
Write-Host 'Storm reticle smoke passed. Stop here for manual visual review.'
