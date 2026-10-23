# Bounded Korriban-only recovery. Does not touch the A4 inspector or another planet's output.
$ErrorActionPreference='Stop'
$python='C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$imagePython='C:\Users\brant\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
function Step([string]$Program,[string[]]$Arguments,[string]$Name) {
    & $Program @Arguments *> (Join-Path $PSScriptRoot "logs/d4-$Name.log")
    if($LASTEXITCODE -ne 0){throw "D4 $Name failed; see logs/d4-$Name.log"}
}
Step $python @((Join-Path $PSScriptRoot 'scripts/d4_source.py'),'enemies') 'enemies-prepare'
Step $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $PSScriptRoot 'scripts/convert_d4_enemies.py')) 'enemies-models'
Step $python @((Join-Path $PSScriptRoot 'scripts/d4_source.py'),'prepare') 'environment-prepare'
Step $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $PSScriptRoot 'scripts/d4_source.py'),'--','models') 'environment-models'
Step $imagePython @((Join-Path $PSScriptRoot 'scripts/d4_source.py'),'terrain') 'environment-terrain'
Step $python @((Join-Path $PSScriptRoot 'scripts/stage_d4.py')) 'stage'
Step $imagePython @((Join-Path $PSScriptRoot 'scripts/prepare_d4_audio.py')) 'audio'
Step $imagePython @((Join-Path $PSScriptRoot 'scripts/prepare_d4_fx.py')) 'fx'
Step 'dotnet' @('build',(Join-Path $PSScriptRoot 'godot/SwtorAssetInspection.csproj'),'--no-restore','--nologo','-p:UseSharedCompilation=false') 'build'
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/d4-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    Step $godot @('--headless','--editor','--path',(Join-Path $PSScriptRoot 'godot'),'--import') 'import-console'
    Step $godot @('--headless','--path',(Join-Path $PSScriptRoot 'godot'),'--script','res://BuildD4.gd','--quit-after','600') 'environment-build-console'
} finally {$env:APPDATA=$previousAppData}
$receipt=Get-Content (Join-Path $PSScriptRoot 'manifests/d4-environment-godot-build.json') -Raw | ConvertFrom-Json
if(!$receipt.passed){throw 'D4 environment build failed'}
Write-Host 'D4 selected content built. Run the focused D4 smoke, then stop for manual review.'
