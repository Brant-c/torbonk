$ErrorActionPreference='Stop'
& dotnet build "$PSScriptRoot/godot/SwtorAssetInspection.csproj" --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/operations-ui-user'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $log=Join-Path $PSScriptRoot 'logs/operations-ui-smoke.log'
    & "$PSScriptRoot/tools/godot/Godot_console.exe" --path "$PSScriptRoot/godot" --resolution 1600x900 res://Integration/OperationsUiSmoke.tscn --log-file "$PSScriptRoot/logs/operations-ui-runtime.log" *> $log
    if($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'OPERATIONS_UI PASS')){throw "Operations UI check failed: $log"}
    Write-Host 'Operations selection, model preview and regular loading transition passed.'
} finally { $env:APPDATA=$previousAppData }
