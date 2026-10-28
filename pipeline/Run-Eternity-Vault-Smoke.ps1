param(
    [ValidateSet('sorcerer','juggernaut','marauder','assassin','operative','sniper','mercenary','powertech')]
    [string[]]$Class = @('sorcerer','juggernaut','marauder','assassin','operative','sniper','mercenary','powertech'),
    [switch]$SkipImport,
    [switch]$Rendered
)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if($LASTEXITCODE -ne 0){throw 'C# build failed'}
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/eternity-encounter-import-runtime.log') *> (Join-Path $PSScriptRoot 'logs/eternity-encounter-import.log')
    if($LASTEXITCODE -ne 0){throw 'Asset import failed'}
}
foreach($className in $Class){
    $arguments=@('--path',$project,'res://Gameplay/GameWorld_EternityVault.tscn','--log-file',(Join-Path $PSScriptRoot "logs/eternity-encounter-$className-runtime.log"))
    if(!$Rendered){$arguments=@('--headless')+$arguments}
    $arguments+=@('--',"--advanced-class=$className",'--map=eternity_vault','--eternity-encounter-smoke')
    $log=Join-Path $PSScriptRoot "logs/eternity-encounter-$className-smoke.log"
    & $godot @arguments *> $log
    if($LASTEXITCODE -ne 0){throw "$className encounter check failed; see $log"}
    $receipt=Get-Content -LiteralPath (Join-Path $PSScriptRoot "manifests/eternity-encounter-$className-smoke.json") -Raw | ConvertFrom-Json
    if(!$receipt.passed -or !(Select-String -LiteralPath $log -SimpleMatch 'ETERNITY_ENCOUNTER_SMOKE_PASSED' -Quiet)){throw "$className encounter check failed; see $log"}
    Write-Host "${className}: level 1-50, milestones, XRR-3 and restart passed"
}
