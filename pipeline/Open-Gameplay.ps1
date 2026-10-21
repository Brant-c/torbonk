param([switch]$Editor)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Godot C# build failed' }
$arguments = @('--path', ('"' + $project + '"'), 'res://Gameplay/GameWorld.tscn', '--log-file', ('"' + (Join-Path $PSScriptRoot 'logs/gameplay-interactive.log') + '"'))
if ($Editor) { $arguments += '--editor' }
# Explicit user launcher: show the interactive game/editor window.
Start-Process -FilePath (Join-Path $PSScriptRoot 'tools/godot/Godot.exe') -ArgumentList $arguments -WindowStyle Normal
