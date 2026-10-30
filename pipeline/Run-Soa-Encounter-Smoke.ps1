param([string[]]$Classes=@('sorcerer','assassin','juggernaut','marauder','sniper','operative','mercenary','powertech'),[ValidateSet('Normal','Hard','Nightmare')][string]$Difficulty='Hard',[switch]$Rendered)
$ErrorActionPreference='Stop'
& dotnet build "$PSScriptRoot/godot/SwtorAssetInspection.csproj" --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$savedAppData=$env:APPDATA
try {
    foreach($class in $Classes) {
        $env:APPDATA=Join-Path $PSScriptRoot "work/soa-smoke-$class-$Difficulty"
        New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
        $log=Join-Path $PSScriptRoot "logs/soa-$class-$Difficulty-smoke.log"
        $arguments=@('--path',"$PSScriptRoot/godot",'--log-file',"$PSScriptRoot/logs/soa-$class-$Difficulty-runtime.log",'res://Gameplay/GameWorld_SoaP3.tscn','--',"--advanced-class=$class",'--map=soa_p3',"--operation-difficulty=$Difficulty",'--soa-encounter-smoke')
        if(!$Rendered){$arguments=@('--headless')+$arguments}
        & "$PSScriptRoot/tools/godot/Godot_console.exe" @arguments *> $log
        if($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'SOA_ENCOUNTER_SMOKE_PASSED')){throw "Soa check failed: $log"}
        Write-Host "Soa $class $Difficulty passed."
    }
} finally { $env:APPDATA=$savedAppData }
