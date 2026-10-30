param([switch]$SkipImport,[string[]]$Classes,[string[]]$Variants)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Slot policy build failed'}
if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/class-slots-import.log');if($LASTEXITCODE -ne 0){throw 'Import failed'}}
$previousSlotsAppData=$env:APPDATA
try{
 $runs=@('sorcerer','juggernaut','marauder','assassin','operative','sniper')|ForEach-Object {@{class=$_;variant='base'}}
 $runs+=@(@{class='assassin';variant='deception'},@{class='operative';variant='counterstrike'})
 if($Classes){$runs=@($runs|Where-Object {$_.class -in $Classes})}
 if($Variants){$runs=@($runs|Where-Object {$_.variant -in $Variants})}
 foreach($run in $runs){
  $class=$run.class;$variant=$run.variant
  $env:APPDATA=Join-Path $PSScriptRoot ('work/class-slots-'+$class+'-'+[guid]::NewGuid().ToString('N'))
  $profileDir=Join-Path $env:APPDATA 'Godot/app_userdata/SWTOR Survivors - Static Asset Inspection'
  New-Item -ItemType Directory -Force $profileDir|Out-Null
  $ranks=@{};if($variant -eq 'deception'){$ranks['deception.thrashing_blades']=1};if($variant -eq 'counterstrike'){$ranks['concealment.flanking']=1}
  $profiles=@{};$profiles[$class]=$ranks
  @{SchemaVersion=1;Profiles=$profiles}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $profileDir 'talent-profiles.json')
  $log=Join-Path $PSScriptRoot "logs/class-slots-$class-$variant.log"
  & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1500 -- --class-slots-smoke "--advanced-class=$class" "--slots-variant=$variant" *> $log
  $r=Get-Content (Join-Path $PSScriptRoot "manifests/class-slots-$class-$variant.json") -Raw|ConvertFrom-Json
  if($LASTEXITCODE -ne 0 -or !$r.passed -or (Get-Content $log -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "$class/$variant slots failed: $($r.failure)"}
  Write-Host "$class/$variant passed $($r.checks.Count) checks."
 }
}finally{$env:APPDATA=$previousSlotsAppData}
