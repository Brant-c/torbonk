param([switch]$Preview)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$env:TORBONK_MAP = 'soa_p3'
$python = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$imagePython = 'C:\Users\brant\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$godot = Join-Path $root 'tools/godot/Godot_console.exe'
$project = Join-Path $root 'godot'
function Invoke-Step($program, $arguments, $step) {
    $log = Join-Path $root "logs/soa-p3-$step.log"
    & $program @arguments *> $log
    if ($LASTEXITCODE -ne 0) { throw "$step failed; see $log" }
    if ((Get-Content $log -Raw) -match 'SCRIPT ERROR:|Parse Error:|MAP_BUILD_FAILED') { throw "$step failed; see $log" }
}
Invoke-Step $python @("$root/scripts/map_prepare.py") 'prepare'
Invoke-Step $imagePython @("$root/scripts/soa_room_source.py") 'room-source'
Invoke-Step $blender @('--background','--factory-startup','--python-exit-code','1','--python',"$root/scripts/map_models.py") 'models'
Invoke-Step $imagePython @("$root/scripts/map_terrain.py") 'terrain'
Invoke-Step $imagePython @("$root/scripts/eternity_terrain_detail.py") 'terrain-detail'
Invoke-Step $godot @('--headless','--editor','--path',$project,'--import','--log-file',"$root/logs/soa-p3-import-runtime.log") 'import'
Invoke-Step $godot @('--headless','--path',$project,'--log-file',"$root/logs/soa-p3-build-runtime.log",'--script','res://BuildMap.gd','--','--map=soa_p3') 'build'
Invoke-Step $godot @('--headless','--path',$project,'--log-file',"$root/logs/soa-p3-check-runtime.log",'res://Integration/SoaP3Import.tscn','--','--soa-import-check') 'check'
if ($Preview) {
    & $godot --path $project --log-file "$root/logs/soa-p3-preview-runtime.log" res://Integration/SoaP3Import.tscn
}
