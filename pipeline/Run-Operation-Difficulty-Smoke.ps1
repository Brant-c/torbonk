$ErrorActionPreference='Stop'
& dotnet build "$PSScriptRoot/godot/SwtorAssetInspection.csproj" --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/operation-difficulty-user'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach($mode in @('Normal','Hard','Nightmare')) {
        $log=Join-Path $PSScriptRoot "logs/operation-difficulty-$mode.log"
        & "$PSScriptRoot/tools/godot/Godot_console.exe" --path "$PSScriptRoot/godot" --resolution 1600x900 res://Integration/OperationsUiSmoke.tscn --log-file "$PSScriptRoot/logs/operation-difficulty-$mode-runtime.log" -- "--test-operation-mode=$mode" --verify-operation-tuning *> $log
        if($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch "OPERATION_DIFFICULTY $mode PASS")){throw "Operation difficulty check failed: $log"}
        Write-Host "$mode passed selection, loading, enemy and boss tuning, dodge and restart checks."
    }
} finally { $env:APPDATA=$previousAppData }
