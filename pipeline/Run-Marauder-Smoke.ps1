param([switch]$SkipImport)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Marauder build failed'}
if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/marauder-import.log');if($LASTEXITCODE -ne 0){throw 'Marauder import failed'}}
$previousAppData=$env:APPDATA
try {
 $env:APPDATA=Join-Path $PSScriptRoot ('work/marauder-smoke-'+[guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
 $log=Join-Path $PSScriptRoot 'logs/marauder-smoke.log'
 & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1000 -- --marauder-smoke --advanced-class=marauder *> $log
 if($LASTEXITCODE -ne 0){throw 'Marauder runtime failed'}
 $report=Get-Content (Join-Path $PSScriptRoot 'manifests/marauder-smoke.json') -Raw | ConvertFrom-Json
 if(!$report.passed -or (Get-Content $log -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "Marauder smoke failed: $($report.failure)"}
 Write-Host "Marauder passed $($report.checks.Count) checks."
}finally {$env:APPDATA=$previousAppData}
