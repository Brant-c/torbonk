param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B5 C# build failed' }
$runtimeLog = Join-Path $PSScriptRoot 'logs/abilities-smoke-runtime.log'
& (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file $runtimeLog --quit-after 2400 -- --abilities-smoke
if ($LASTEXITCODE -ne 0) { throw 'B5 runtime smoke failed; see logs/abilities-smoke-runtime.log' }
$receipt = Join-Path $PSScriptRoot 'manifests/abilities-smoke.json'
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B5 smoke incomplete or failed' }
if ((Get-Content -LiteralPath $runtimeLog -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'B5 runtime reported errors; see log' }
Write-Host 'B5 smoke passed. Stop here for manual review.'
