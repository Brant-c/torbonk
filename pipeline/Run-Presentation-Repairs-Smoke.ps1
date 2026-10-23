param([switch]$SkipBuild,[switch]$Rendered,[string[]]$Profiles=@('sorcerer','mercenary','sniper','operative','assassin','marauder','juggernaut','powertech'))
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
if(!$SkipBuild){
    & dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
    if($LASTEXITCODE -ne 0){throw 'Presentation repair build failed'}
}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/presentation-repairs-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach($profile in $Profiles){
        $log=Join-Path $PSScriptRoot "logs/presentation-repairs-$profile.log"
        $console=Join-Path $PSScriptRoot "logs/presentation-repairs-$profile-console.log"
        $receipt=Join-Path $PSScriptRoot "manifests/presentation-repairs-$profile-smoke.json"
        $started=Get-Date
        $engineArgs=@('--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','600','--log-file',$log)
        if(!$Rendered){$engineArgs=@('--headless')+$engineArgs}
        $smokeArgs=@('--presentation-repair-smoke',"--advanced-class=$profile")
        if($Rendered){$smokeArgs+='--presentation-repair-captures'}
        & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') @engineArgs -- @smokeArgs *> $console
        if($LASTEXITCODE -ne 0){throw "$profile runtime failed; see $console"}
        if(!(Test-Path $receipt) -or (Get-Item $receipt).LastWriteTime -lt $started){throw "$profile did not write a fresh receipt"}
        $result=Get-Content $receipt -Raw | ConvertFrom-Json
        if(!$result.passed){throw "$profile failed: $($result.failure)"}
        if((Get-Content $console -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw "$profile runtime errors; see $console"}
        Write-Host "$profile passed $($result.checks.Count) checks"
    }
} finally {$env:APPDATA=$previousAppData}
