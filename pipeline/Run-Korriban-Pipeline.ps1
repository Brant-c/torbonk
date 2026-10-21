param([switch]$Capture, [switch]$SkipRegressions, [switch]$SkipArchiveRehash)
$ErrorActionPreference = 'Stop'
$pipelineRoot = $PSScriptRoot
$python = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$imagePython = 'C:\Users\brant\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$godot = Join-Path $pipelineRoot 'tools\godot\Godot_console.exe'
$project = Join-Path $pipelineRoot 'godot'
function Invoke-KorribanChecked([string]$Program, [string[]]$Arguments, [string]$Name) {
    $logPath = Join-Path $pipelineRoot "logs\$Name.log"
    & $Program @Arguments *> $logPath
    if ($LASTEXITCODE -ne 0) { throw "$Name failed ($LASTEXITCODE); see $logPath" }
    if ((Get-Content -LiteralPath $logPath -Raw) -match 'SCRIPT ERROR:|Parse Error:|KORRIBAN_\w+_FAILED') { throw "$Name reported errors; see $logPath" }
}
& (Join-Path $pipelineRoot 'Assert-Toolchain.ps1')
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'snapshot') 'korriban-snapshot'
Invoke-KorribanChecked $python @((Join-Path $pipelineRoot 'scripts\korriban_source.py')) 'korriban-area-source'
Invoke-KorribanChecked $python @((Join-Path $pipelineRoot 'scripts\prepare_korriban.py')) 'korriban-prepare'
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'ste2-input') 'korriban-ste2-input'
$compatProject = Join-Path $pipelineRoot 'scripts\ste2_compat\Ste2Test.csproj'
Invoke-KorribanChecked 'dotnet' @('build',$compatProject,'--nologo','-p:BaseOutputPath=../../work/korriban/ste2-bin/','-p:BaseIntermediateOutputPath=../../work/korriban/ste2-obj/') 'korriban-ste2-build'
$compatDll = Join-Path $pipelineRoot 'work\korriban\ste2-bin\Debug\net8.0-windows\Ste2Test.dll'
Invoke-KorribanChecked 'dotnet' @($compatDll,(Join-Path $pipelineRoot 'work\korriban\ste2-input'),(Join-Path $pipelineRoot 'work\korriban\ste2-output')) 'korriban-ste2-run'
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'ste2-compare') 'korriban-ste2-compare'
Invoke-KorribanChecked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\convert_korriban_models.py')) 'korriban-models'
Invoke-KorribanChecked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $pipelineRoot 'scripts\validate_korriban_models.py')) 'korriban-roundtrip'
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\stage_korriban_terrain.py')) 'korriban-terrain'
$runtime = Join-Path $pipelineRoot 'logs\korriban-runtime.log'
Invoke-KorribanChecked $godot @('--headless','--editor','--path',$project,'--import','--log-file',$runtime) 'korriban-import'
Invoke-KorribanChecked $godot @('--headless','--path',$project,'--script','res://BuildKorriban.gd','--log-file',$runtime) 'korriban-build'
if (!(Get-Content (Join-Path $pipelineRoot 'manifests\korriban-godot-build.json') -Raw | ConvertFrom-Json).passed) { throw 'Environment scene build validation failed' }
Invoke-KorribanChecked $godot @('--headless','--path',$project,'res://KorribanInspection.tscn','--quit-after','3600','--log-file',$runtime,'--','--korriban-validate') 'korriban-validation'
if ($Capture) {
    Invoke-KorribanChecked $godot @('--path',$project,'res://KorribanInspection.tscn','--quit-after','3600','--log-file',$runtime,'--','--korriban-capture') 'korriban-capture'
}
if (!$SkipRegressions) {
    & (Join-Path $pipelineRoot 'Run-Lightning-Pipeline.ps1') -Capture:$Capture -SkipArchiveRehash:$SkipArchiveRehash
}
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'verify') 'korriban-source-integrity'
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'artifacts') 'korriban-artifact-hashes'
Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'repeat') 'korriban-repeat'
[ordered]@{passed=$true; regressionThisRun=!$SkipRegressions; archiveRehashThisRun=(!$SkipRegressions -and !$SkipArchiveRehash); capturesThisRun=[bool]$Capture} | ConvertTo-Json | Set-Content (Join-Path $pipelineRoot 'manifests\korriban-run.json')
if ($Capture -and !$SkipRegressions -and !$SkipArchiveRehash) {
    Invoke-KorribanChecked $imagePython @((Join-Path $pipelineRoot 'scripts\record_korriban.py'),'acceptance') 'korriban-acceptance'
}
Write-Host 'Milestone A4 conversion and inspection checks passed. Stop before gameplay.'
