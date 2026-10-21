param([switch]$Editor)
$pipelineRoot = $PSScriptRoot
$enginePath = Join-Path $pipelineRoot 'tools\godot\Godot.exe'
$launchArguments = @('--path', ('"' + (Join-Path $pipelineRoot 'godot') + '"'), '--log-file', ('"' + (Join-Path $pipelineRoot 'logs\godot-interactive.log') + '"'))
if ($Editor) { $launchArguments += '--editor' }
# This launcher is explicitly for the user's visible, interactive inspection.
Start-Process -FilePath $enginePath -ArgumentList $launchArguments -WindowStyle Normal
