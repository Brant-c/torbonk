param([Parameter(Mandatory)][ValidateSet('hutta','dromund','balmorra')][string]$Map)
# Extract -> convert -> stage -> Godot import/build for one TorBonk map section (see MAPS-README.md).
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$python = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$imagePython = 'C:\Users\brant\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$godot = Join-Path $root 'tools\godot\Godot_console.exe'
$project = Join-Path $root 'godot'
$env:TORBONK_MAP = $Map
function Invoke-Checked([string]$Program, [string[]]$Arguments, [string]$Name) {
    $log = Join-Path $root "logs\map-$Map-$Name.log"
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    & $Program @Arguments *> $log
    $code = $LASTEXITCODE; $ErrorActionPreference = $previous
    if ($code -ne 0) { throw "$Name failed ($code); see $log" }
    if ((Get-Content -LiteralPath $log -Raw) -match 'SCRIPT ERROR:|Parse Error:|MAP_\w+_FAILED') { throw "$Name reported errors; see $log" }
}
Invoke-Checked $python @((Join-Path $root 'scripts\map_prepare.py')) 'prepare'
Invoke-Checked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $root 'scripts\map_models.py')) 'models'
Invoke-Checked $imagePython @((Join-Path $root 'scripts\map_terrain.py')) 'terrain'
Invoke-Checked $godot @('--headless','--editor','--path',$project,'--import','--log-file',(Join-Path $root "logs\map-$Map-import-runtime.log")) 'import'
Invoke-Checked $godot @('--headless','--path',$project,'--script','res://BuildMap.gd','--log-file',(Join-Path $root "logs\map-$Map-build-runtime.log"),'--',"--map=$Map") 'build'
Write-Host "Map $Map built."
