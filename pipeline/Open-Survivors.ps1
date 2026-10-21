param([switch]$Editor, [switch]$WardrobeDev)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Godot C# build failed' }
$arguments = @('--path', ('"' + $project + '"'), '--log-file', ('"' + (Join-Path $PSScriptRoot 'logs/survivors-interactive.log') + '"'))
if ($Editor) { $arguments += '--editor' }
if ($WardrobeDev) { $arguments += @('--','--wardrobe-dev') }
# Explicit user launcher: show the complete game/editor window. F5 starts the main menu.
Start-Process -FilePath (Join-Path $PSScriptRoot 'tools/godot/Godot.exe') -ArgumentList $arguments -WindowStyle Normal
