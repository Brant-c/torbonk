param([switch]$SkipArchiveRehash, [switch]$Capture)
$ErrorActionPreference = 'Stop'
$pipelineRoot = $PSScriptRoot
$blenderPath = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$pythonPath = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$dotnetPath = 'C:\Program Files\dotnet\dotnet.exe'
$godotPath = Join-Path $pipelineRoot 'tools\godot\Godot_console.exe'
$projectPath = Join-Path $pipelineRoot 'godot'

function Invoke-Checked([string]$Program, [string[]]$Arguments, [string]$LogName) {
    Write-Host "Running $LogName"
    & $Program @Arguments *> (Join-Path $pipelineRoot "logs\$LogName.log")
    if ($LASTEXITCODE -ne 0) { throw "$LogName failed ($LASTEXITCODE). See pipeline/logs/$LogName.log" }
}

& (Join-Path $pipelineRoot 'Assert-Toolchain.ps1')
$glbPath = Join-Path $pipelineRoot 'generated\sith_academy_door.glb'
$previousHash = if (Test-Path $glbPath) { (Get-FileHash $glbPath -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
Invoke-Checked $pythonPath @((Join-Path $pipelineRoot 'scripts\source_pipeline.py'),'inventory') 'source-inventory'
Invoke-Checked $pythonPath @((Join-Path $pipelineRoot 'scripts\source_pipeline.py'),'extract') 'source-extract'
Invoke-Checked $blenderPath @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\convert_door.py')) 'blender-conversion'
Invoke-Checked $pythonPath @((Join-Path $pipelineRoot 'scripts\validate_glb.py')) 'glb-validation'
Invoke-Checked $blenderPath @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\validate_roundtrip.py')) 'roundtrip-validation'
Copy-Item -LiteralPath $glbPath -Destination (Join-Path $projectPath 'Assets\sith_academy_door.glb')
Copy-Item -LiteralPath (Join-Path $pipelineRoot 'recipe.json') -Destination (Join-Path $projectPath 'Assets\recipe.json')
Invoke-Checked $dotnetPath @('build',(Join-Path $projectPath 'SwtorAssetInspection.csproj'),'--ignore-failed-sources') 'dotnet-build'
$runtimeLog = Join-Path $pipelineRoot 'logs\godot-runtime.log'
Invoke-Checked $godotPath @('--headless','--editor','--path',$projectPath,'--import','--log-file',$runtimeLog) 'godot-import'
Invoke-Checked $godotPath @('--headless','--path',$projectPath,'--log-file',$runtimeLog,'--','--validate') 'godot-validation'
if ($Capture) {
    Invoke-Checked $godotPath @('--path',$projectPath,'--log-file',$runtimeLog,'--','--capture',(Join-Path $pipelineRoot 'generated\godot-inspection.png')) 'godot-capture'
    Invoke-Checked $godotPath @('--path',$projectPath,'--log-file',$runtimeLog,'--','--view','3.42','--capture',(Join-Path $pipelineRoot 'generated\godot-side-inspection.png')) 'godot-side-capture'
}
if (!$SkipArchiveRehash) {
    Invoke-Checked $pythonPath @((Join-Path $pipelineRoot 'scripts\source_pipeline.py'),'verify') 'source-integrity'
}
$newHash = (Get-FileHash $glbPath -Algorithm SHA256).Hash.ToLowerInvariant()
$repeat = [ordered]@{
    previousGlbSha256 = $previousHash
    newGlbSha256 = $newHash
    byteIdenticalGlb = ($previousHash -eq $newHash)
    allStagesPassed = $true
    archiveRehashThisRun = !$SkipArchiveRehash
    note = 'GLB determinism only; Blender files and rendered images are recorded outputs, not required to be byte-identical.'
}
$repeat | ConvertTo-Json | Set-Content (Join-Path $pipelineRoot 'manifests\repeat-validation.json')
if ($previousHash -and $newHash -ne $previousHash) { throw 'GLB changed on repeat conversion; inspect repeat-validation.json' }
Invoke-Checked $pythonPath @((Join-Path $pipelineRoot 'scripts\record_artifacts.py')) 'artifact-hashes'
Write-Host 'Static prop pipeline passed. Tasks 1-3 only.'

