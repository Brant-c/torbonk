param([switch]$Editor)
$enginePath = Join-Path $PSScriptRoot 'tools\godot\Godot.exe'
$launchArguments = @('--path', ('"' + (Join-Path $PSScriptRoot 'godot') + '"'), 'res://CharacterInspection.tscn', '--log-file', ('"' + (Join-Path $PSScriptRoot 'logs\character-interactive.log') + '"'))
if ($Editor) { $launchArguments += '--editor' }
# This launcher is explicitly for the user's visible interactive inspector.
Start-Process -FilePath $enginePath -ArgumentList $launchArguments -WindowStyle Normal
