$ErrorActionPreference='Stop'
& dotnet build "$PSScriptRoot/godot/SwtorAssetInspection.csproj" --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/equipment-smoke-user'
    & "$PSScriptRoot/tools/godot/Godot_console.exe" --path "$PSScriptRoot/godot" --resolution 1600x900 res://Integration/EquipmentSmoke.tscn *> "$PSScriptRoot/logs/equipment-smoke.log"
    if($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath "$PSScriptRoot/logs/equipment-smoke.log" -SimpleMatch 'EQUIPMENT PASS')){throw 'Equipment validation failed'}
} finally {$env:APPDATA=$previousAppData}
