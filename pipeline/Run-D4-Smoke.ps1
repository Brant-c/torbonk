param([switch]$SkipImport,[switch]$Capture)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false *> (Join-Path $PSScriptRoot 'logs/d4-build.log')
if($LASTEXITCODE -ne 0){throw 'D4 C# build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/d4-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    if(!$SkipImport){
        & $godot --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/d4-import.log') *> (Join-Path $PSScriptRoot 'logs/d4-import-console.log')
        if($LASTEXITCODE -ne 0){throw 'D4 asset import failed'}
    }
    $options=@('--d4-smoke','--advanced-class=juggernaut');if($Capture){$options+='--d4-capture'}
    & $godot --path $project res://Gameplay/GameWorld.tscn --max-fps 60 --quit-after 900 --log-file (Join-Path $PSScriptRoot 'logs/d4-runtime.log') -- @options *> (Join-Path $PSScriptRoot 'logs/d4-runtime-console.log')
    if($LASTEXITCODE -ne 0){throw 'D4 runtime smoke failed'}
} finally {$env:APPDATA=$previousAppData}
$receipt=Get-Content (Join-Path $PSScriptRoot 'manifests/d4-smoke.json') -Raw | ConvertFrom-Json
if(!$receipt.passed){throw 'D4 focused checks failed'}
if((Get-Content (Join-Path $PSScriptRoot 'logs/d4-runtime-console.log') -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw 'D4 runtime errors'}
Write-Host 'D4 passed. Stop for manual Korriban gameplay and visual review.'
