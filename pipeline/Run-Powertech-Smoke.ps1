param()
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Powertech C# build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 500 --log-file (Join-Path $PSScriptRoot 'logs/powertech-smoke.log') -- --powertech-smoke --advanced-class=powertech *> (Join-Path $PSScriptRoot 'logs/powertech-smoke-console.log')
$receipt=Get-Content (Join-Path $PSScriptRoot 'manifests/powertech-smoke.json') -Raw | ConvertFrom-Json
if($LASTEXITCODE -ne 0 -or !$receipt.passed){throw "Powertech smoke failed: $($receipt.failure)"}
Write-Host "Powertech: $($receipt.checks.Count) checks passed. Stop for manual gameplay and visual review."
