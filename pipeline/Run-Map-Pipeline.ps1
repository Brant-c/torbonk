param([Parameter(Mandatory)][ValidateSet('hutta','dromund','balmorra','eternity_vault')][string]$Map)
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
if ($Map -eq 'eternity_vault') { Invoke-Checked $imagePython @((Join-Path $root 'scripts\eternity_refine_source.py')) 'refine-source' }
if ($Map -eq 'eternity_vault') { Invoke-Checked $imagePython @((Join-Path $root 'scripts\eternity_authored.py')) 'authored-source' }
Invoke-Checked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $root 'scripts\map_models.py')) 'models'
Invoke-Checked $imagePython @((Join-Path $root 'scripts\map_terrain.py')) 'terrain'
if ($Map -eq 'eternity_vault') { Invoke-Checked $imagePython @((Join-Path $root 'scripts\eternity_terrain_detail.py')) 'detail-terrain' }
if ($Map -eq 'eternity_vault') { Invoke-Checked $imagePython @((Join-Path $root 'scripts\eternity_atmosphere_source.py')) 'atmosphere-source' }
if ($Map -eq 'eternity_vault') { Invoke-Checked $imagePython @((Join-Path $root 'scripts\trace_ev_surface_fx.py')) 'fx-trace' }
if ($Map -eq 'eternity_vault') { Invoke-Checked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $root 'scripts\convert_ev_sky.py')) 'sky-geometry' }
if ($Map -eq 'eternity_vault') {
    Invoke-Checked $imagePython @((Join-Path $root 'scripts\restore_ev_fidelity.py')) 'surface-fidelity-source'
    Invoke-Checked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $root 'scripts\convert_ev_horizon.py')) 'surface-horizon'
}
if ($Map -eq 'eternity_vault') { Invoke-Checked $python @((Join-Path $root 'scripts\make_map_world.py'),$Map) 'world' }
Invoke-Checked $godot @('--headless','--editor','--path',$project,'--import','--log-file',(Join-Path $root "logs\map-$Map-import-runtime.log")) 'import'
Invoke-Checked $godot @('--headless','--path',$project,'--script','res://BuildMap.gd','--log-file',(Join-Path $root "logs\map-$Map-build-runtime.log"),'--',"--map=$Map") 'build'
Write-Host "Map $Map built."
