param([switch]$Editor)
$ErrorActionPreference = 'Stop'
$engine = Join-Path $PSScriptRoot 'tools\godot\Godot.exe'
$arguments = @('--path', (Join-Path $PSScriptRoot 'godot'))
if ($Editor) { $arguments += '--editor' }
$arguments += 'res://LightningInspection.tscn'
Start-Process -FilePath $engine -ArgumentList $arguments -WindowStyle Normal
