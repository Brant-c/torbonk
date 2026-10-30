param()
$ErrorActionPreference='Stop'
& dotnet build "$PSScriptRoot/godot/SwtorAssetInspection.csproj" --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$savedAppData=$env:APPDATA
try {
    foreach($map in @('soa_p3','eternity_vault')) {
        $scene=if($map -eq 'soa_p3'){'GameWorld_SoaP3'}else{'GameWorld_EternityVault'}
        foreach($mode in @('Normal','Hard','Nightmare')) {
            $env:APPDATA=Join-Path $PSScriptRoot "work/continuous-$map-$mode"
            New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
            $log=Join-Path $PSScriptRoot "logs/continuous-$map-$mode.log"
            & "$PSScriptRoot/tools/godot/Godot_console.exe" --headless --path "$PSScriptRoot/godot" --log-file "$PSScriptRoot/logs/continuous-$map-$mode-runtime.log" "res://Gameplay/$scene.tscn" -- --advanced-class=sorcerer "--map=$map" "--operation-difficulty=$mode" --continuous-encounter-smoke *> $log
            if($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'CONTINUOUS_ENCOUNTER_PASSED')){throw "Continuous encounter check failed: $log"}
            Write-Host "$map $mode continuous pressure passed."
        }
    }
} finally { $env:APPDATA=$savedAppData }
