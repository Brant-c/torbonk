param([switch]$SkipImport)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'D3 C# build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/d3-import.log') *> (Join-Path $PSScriptRoot 'logs/d3-import-console.log')
    if($LASTEXITCODE -ne 0){throw 'D3 import failed'}
}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/d3-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    & $godot --path $project res://Gameplay/GameWorld.tscn --max-fps 60 --quit-after 1800 --log-file (Join-Path $PSScriptRoot 'logs/d3-runtime.log') -- --d3-smoke --advanced-class=mercenary *> (Join-Path $PSScriptRoot 'logs/d3-runtime-console.log')
    if($LASTEXITCODE -ne 0){throw 'D3 runtime smoke failed'}
} finally {$env:APPDATA=$previousAppData}
$receipt=Get-Content (Join-Path $PSScriptRoot 'manifests/d3-smoke.json') -Raw | ConvertFrom-Json
if(!$receipt.passed){throw 'D3 checks failed'}
if((Get-Content (Join-Path $PSScriptRoot 'logs/d3-runtime-console.log') -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw 'D3 runtime errors'}
Write-Host 'D3 passed. Stop for manual class review.'
