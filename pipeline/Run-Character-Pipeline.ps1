param([switch]$Capture, [switch]$SkipArchiveRehash)
$ErrorActionPreference = 'Stop'
$pipelineRoot = $PSScriptRoot
$blenderPath = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$pythonPath = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$godotPath = Join-Path $pipelineRoot 'tools\godot\Godot_console.exe'
$projectPath = Join-Path $pipelineRoot 'godot'
$runtimeLog = Join-Path $pipelineRoot 'logs\character-godot-runtime.log'
function Invoke-CharacterChecked([string]$Program, [string[]]$Arguments, [string]$LogName) {
    Write-Host "Running $LogName"
    & $Program @Arguments *> (Join-Path $pipelineRoot "logs\$LogName.log")
    if ($LASTEXITCODE -ne 0) { throw "$LogName failed ($LASTEXITCODE); see pipeline/logs/$LogName.log" }
}
& (Join-Path $pipelineRoot 'Assert-Toolchain.ps1')
$glbPath = Join-Path $pipelineRoot 'generated\sorcerer.glb'
$previousHash = if (Test-Path $glbPath) { (Get-FileHash -LiteralPath $glbPath -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
Invoke-CharacterChecked $pythonPath @((Join-Path $pipelineRoot 'scripts\select_character.py')) 'character-selection'
Invoke-CharacterChecked $blenderPath @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\convert_character.py')) 'character-conversion'
Invoke-CharacterChecked $pythonPath @((Join-Path $pipelineRoot 'scripts\validate_character_glb.py')) 'character-glb-validation'
Invoke-CharacterChecked $blenderPath @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\validate_character_roundtrip.py')) 'character-roundtrip-validation'
Invoke-CharacterChecked $pythonPath @((Join-Path $pipelineRoot 'scripts\stage_character.py')) 'character-stage'
Invoke-CharacterChecked $godotPath @('--headless','--editor','--path',$projectPath,'--import','--log-file',$runtimeLog) 'character-godot-import'
Invoke-CharacterChecked $godotPath @('--headless','--path',$projectPath,'res://CharacterInspection.tscn','--log-file',$runtimeLog,'--','--character-validate') 'character-godot-validation'
if ($Capture) {
    Invoke-CharacterChecked $blenderPath @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\render_character_blender.py')) 'character-blender-render'
    Invoke-CharacterChecked $godotPath @('--path',$projectPath,'res://CharacterInspection.tscn','--log-file',$runtimeLog,'--','--character-capture') 'character-godot-capture'
}
Invoke-CharacterChecked $pythonPath @((Join-Path $pipelineRoot 'scripts\character_source.py'),'verify') 'character-source-integrity'
# Reuse the complete successful static pipeline, including C# build and engine
# checks. Character work must not replace or weaken the doorway regression.
& (Join-Path $pipelineRoot 'Run-Pipeline.ps1') -SkipArchiveRehash:$SkipArchiveRehash -Capture:$Capture
$newHash = (Get-FileHash -LiteralPath $glbPath -Algorithm SHA256).Hash.ToLowerInvariant()
$result = [ordered]@{
    previousGlbSha256 = $previousHash
    newGlbSha256 = $newHash
    byteIdenticalGlb = ($previousHash -eq $newHash)
    passed = (!$previousHash -or $previousHash -eq $newHash)
    doorwayRegressionPassed = $true
    archiveRehashThisRun = !$SkipArchiveRehash
    capturesThisRun = [bool]$Capture
}
$result | ConvertTo-Json | Set-Content (Join-Path $pipelineRoot 'manifests\character-repeat-validation.json')
if (!$result.passed) { throw 'Character GLB changed on repeat; inspect character-repeat-validation.json' }
Invoke-CharacterChecked $pythonPath @((Join-Path $pipelineRoot 'scripts\record_character_artifacts.py')) 'character-artifact-hashes'
Write-Host 'Tasks 4-5 character checks and doorway regression passed. Stop before terrain, effects or gameplay.'
