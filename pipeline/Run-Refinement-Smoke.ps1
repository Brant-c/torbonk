param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B5.1 C# build failed' }
$runtimeLog = Join-Path $PSScriptRoot 'logs/refinement-smoke-runtime.log'
$refinementAppData = Join-Path $PSScriptRoot 'work/refinement-user'
New-Item -ItemType Directory -Path $refinementAppData -Force | Out-Null
$previousAppData = $env:APPDATA
try {
    $env:APPDATA = $refinementAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file $runtimeLog --quit-after 1200 -- --refinement-smoke
} finally { $env:APPDATA = $previousAppData }
if ($LASTEXITCODE -ne 0) { throw 'B5.1 runtime smoke failed; see logs/refinement-smoke-runtime.log' }
$receipt = Join-Path $PSScriptRoot 'manifests/refinement-smoke.json'
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B5.1 smoke incomplete or failed' }
if ((Get-Content -LiteralPath $runtimeLog -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'B5.1 runtime reported errors; see log' }
Write-Host 'B5.1 smoke passed. Stop here for manual gameplay and visual review.'
