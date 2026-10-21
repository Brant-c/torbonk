param()
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if($LASTEXITCODE -ne 0){throw 'D1 C# build failed'}
$arguments=@('--path',('"'+$project+'"'),'res://Gameplay/GameWorld.tscn','--log-file',('"'+(Join-Path $PSScriptRoot 'logs/d1-review.log')+'"'),'--','--d1-equipment')
# Explicit user launcher for the manual review window.
Start-Process -FilePath (Join-Path $PSScriptRoot 'tools/godot/Godot.exe') -ArgumentList $arguments -WindowStyle Normal
