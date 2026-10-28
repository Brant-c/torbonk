param([switch]$SkipImport,[switch]$Capture)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Warrior movement build failed'}
if(!$SkipImport){
 & $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/warrior-locomotion-import.log')
 if($LASTEXITCODE -ne 0){throw 'Warrior movement import failed'}
}
$previousMovementAppData=$env:APPDATA
try {
 foreach($class in @('marauder','juggernaut')){
  $env:APPDATA=Join-Path $PSScriptRoot ('work/warrior-movement-'+$class+'-'+[guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
  $log=Join-Path $PSScriptRoot ('logs/warrior-movement-'+$class+'.log')
  $runArgs=@('--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','3000','--','--warrior-movement-smoke',('--advanced-class='+$class))
  if($Capture){
   $errLog=Join-Path $PSScriptRoot ('logs/movement-render-'+$class+'-errors.log')
   $runArgs=@('--rendering-method','gl_compatibility')+$runArgs+@('--warrior-movement-captures')
   $process=Start-Process -FilePath $godot -ArgumentList $runArgs -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $log -RedirectStandardError $errLog
   $exitCode=$process.ExitCode
  }else{& $godot --headless @runArgs *> $log;$exitCode=$LASTEXITCODE}
  if($exitCode -ne 0){throw "$class movement runtime failed"}
  $report=Get-Content (Join-Path $PSScriptRoot ('manifests/warrior-movement-'+$class+'.json')) -Raw | ConvertFrom-Json
  $errors=Get-Content $log -Raw
  if($Capture){$errors+=Get-Content $errLog -Raw}
  if(!$report.passed -or $errors -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "$class movement failed: $($report.failure)"}
  Write-Host "$class movement and fallback passed $($report.checks.Count) checks."
 }
}finally{$env:APPDATA=$previousMovementAppData}
