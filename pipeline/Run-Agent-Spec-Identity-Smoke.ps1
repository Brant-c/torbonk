param([string]$ProfileRoot=(Join-Path $PSScriptRoot 'work'))
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Identity build failed'}
$previousIdentityAppData=$env:APPDATA
try{
 foreach($class in @('sniper','operative')){
  $env:APPDATA=Join-Path $ProfileRoot ('agent-identity-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Force $env:APPDATA|Out-Null
  $log=Join-Path $PSScriptRoot "logs/agent-spec-identity-$class.log"
  & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1600 -- --agent-spec-identity-smoke "--advanced-class=$class" *> $log
  $r=Get-Content (Join-Path $PSScriptRoot "manifests/agent-spec-identity-$class.json") -Raw|ConvertFrom-Json
  if($LASTEXITCODE -ne 0 -or !$r.passed -or (Get-Content $log -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "$class identity failed: $($r.failure)"}
  Write-Host "$class identity passed $($r.checks.Count) checks."
 }
}finally{$env:APPDATA=$previousIdentityAppData}
