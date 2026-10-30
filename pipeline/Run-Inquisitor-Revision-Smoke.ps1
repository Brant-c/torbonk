param([switch]$SkipImport,[switch]$Capture)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Inquisitor build failed'}
if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/inquisitor-revision-import.log');if($LASTEXITCODE -ne 0){throw 'Inquisitor import failed'}}
$previousInquisitorAppData=$env:APPDATA
try{
 foreach($class in @('sorcerer','assassin')){
  $env:APPDATA=Join-Path $PSScriptRoot ('work/inquisitor-revision-'+$class+'-'+[guid]::NewGuid().ToString('N'))
  $profileDir=Join-Path $env:APPDATA 'Godot/app_userdata/SWTOR Survivors - Static Asset Inspection'
  New-Item -ItemType Directory -Force $profileDir|Out-Null
  if($class -eq 'assassin'){
   $ranks=@{};$catalog=Get-Content (Join-Path $project 'Gameplay/Talents/assassin.json') -Raw|ConvertFrom-Json
   foreach($talent in $catalog.Talents|Where-Object Tree -eq deception){$ranks[$talent.Id]=1}
   @{SchemaVersion=1;Profiles=@{assassin=$ranks}}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $profileDir 'talent-profiles.json')
  }
  $log=Join-Path $PSScriptRoot ('logs/inquisitor-revision-'+$class+'.log')
  if($Capture){
   $err=Join-Path $PSScriptRoot ('logs/inquisitor-revision-'+$class+'-render-errors.log')
   $proc=Start-Process -FilePath $godot -ArgumentList @('--rendering-method','gl_compatibility','--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','2000','--','--inquisitor-revision-smoke',('--advanced-class='+$class),'--inquisitor-revision-captures') -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $log -RedirectStandardError $err
   if($proc.ExitCode -ne 0 -or (Get-Content $err -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "$class capture failed"}
  }else{& $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 2000 -- --inquisitor-revision-smoke ('--advanced-class='+$class) *> $log}
  $report=Get-Content (Join-Path $PSScriptRoot ('manifests/inquisitor-revision-'+$class+'.json')) -Raw|ConvertFrom-Json
  if($LASTEXITCODE -ne 0 -or !$report.passed -or (Get-Content $log -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "$class revision failed: $($report.failure)"}
  Write-Host "$class revision passed $($report.checks.Count) checks."
 }
}finally{$env:APPDATA=$previousInquisitorAppData}
