param([switch]$SkipImport)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Juggernaut build failed'}
if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/juggernaut-import.log');if($LASTEXITCODE -ne 0){throw 'Juggernaut import failed'}}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot ('work/juggernaut-smoke-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $log=Join-Path $PSScriptRoot 'logs/juggernaut-smoke.log'
    & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1000 -- --juggernaut-smoke --advanced-class=juggernaut *> $log
    if($LASTEXITCODE -ne 0){throw 'Juggernaut runtime failed'}
    $report=Get-Content (Join-Path $PSScriptRoot 'manifests/juggernaut-smoke.json') -Raw | ConvertFrom-Json
    if(!$report.passed -or (Get-Content $log -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "Juggernaut smoke failed: $($report.failure)"}
    Write-Host "Juggernaut passed $($report.checks.Count) checks."
}finally {$env:APPDATA=$previousAppData}
