param([switch]$SkipImport)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Assassin build failed'}
if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/assassin-import.log');if($LASTEXITCODE -ne 0){throw 'Assassin import failed'}}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot ('work/assassin-smoke-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach($stage in @('baseline','hybrid')){
        $log=Join-Path $PSScriptRoot "logs/assassin-$stage.log"
        & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1000 -- --assassin-expansion-smoke --advanced-class=assassin *> $log
        if($LASTEXITCODE -ne 0){throw "Assassin $stage failed"}
        $report=Get-Content (Join-Path $PSScriptRoot 'manifests/assassin-expansion-smoke.json') -Raw | ConvertFrom-Json
        if(!$report.passed -or (Get-Content $log -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "Assassin $stage failed: $($report.failure)"}
        Write-Host "Assassin $stage passed $($report.checks.Count) checks."
    }
}finally {$env:APPDATA=$previousAppData}
