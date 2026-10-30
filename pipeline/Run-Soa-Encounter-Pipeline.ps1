param()
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$python='C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
function Step($program,$arguments,$name) {
    $log=Join-Path $root "logs/soa-encounter-$name.log"
    & $program @arguments *> $log
    if($LASTEXITCODE -ne 0){throw "$name failed: $log"}
    if((Get-Content $log -Raw) -match 'SCRIPT ERROR:|Parse Error:'){throw "$name reported an error: $log"}
}
Step $python @("$root/scripts/prepare_soa_encounter.py") 'prepare'
Step $blender @('--background','--factory-startup','--python-exit-code','1','--python',"$root/scripts/convert_soa_enemies.py") 'convert'
Step $python @("$root/scripts/make_soa_encounter.py") 'compose'
Step 'dotnet' @('build',"$root/godot/SwtorAssetInspection.csproj",'--no-restore','--nologo','-p:UseSharedCompilation=false') 'build'
Step "$root/tools/godot/Godot_console.exe" @('--headless','--editor','--path',"$root/godot",'--import','--log-file',"$root/logs/soa-encounter-import-runtime.log") 'import'
Write-Host 'Soa NPC assets and level 1–50 run rebuilt. Map geometry is maintained by Run-Soa-P3-Import.ps1.'
