param([switch]$Capture, [switch]$SkipRegressions, [switch]$SkipArchiveRehash)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
$godot = Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
$python = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$imagePython = 'C:\Users\brant\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
function Invoke-B1Checked([string]$Program, [string[]]$Arguments, [string]$Name) {
    Write-Host "Validating $Name"
    $log = Join-Path $PSScriptRoot "logs/$Name.log"
    if ($Program -eq $godot) { $Arguments = @('--log-file',(Join-Path $PSScriptRoot "logs/$Name-runtime.log")) + $Arguments }
    & $Program @Arguments *> $log
    if ($LASTEXITCODE -ne 0) { throw "$Name failed ($LASTEXITCODE); see $log" }
    if ((Get-Content -LiteralPath $log -Raw) -match 'SCRIPT ERROR:|Parse Error:|GAMEPLAY_VALIDATION_FAILED') { throw "$Name reported errors; see $log" }
}
New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'generated/gameplay') | Out-Null
& (Join-Path $PSScriptRoot 'Assert-Toolchain.ps1')
Invoke-B1Checked 'dotnet' @('build',(Join-Path $project 'SwtorAssetInspection.csproj'),'--no-restore','--nologo') 'gameplay-build'
Invoke-B1Checked $godot @('--headless','--editor','--path',$project,'--import','--log-file',(Join-Path $PSScriptRoot 'logs/gameplay-import-runtime.log')) 'gameplay-import'
Invoke-B1Checked $godot @('--headless','--path',$project,'res://Gameplay/GameWorld.tscn','--fixed-fps','120','--quit-after','18000','--','--gameplay-validate') 'gameplay-headless'
if (!(Get-Content (Join-Path $PSScriptRoot 'manifests/gameplay-validation.json') -Raw | ConvertFrom-Json).passed) { throw 'B1 runtime checks incomplete' }
if ($Capture) {
    # Real rendering and uncapped simulation clock: the test itself exercises 30/60/144 render caps.
    Invoke-B1Checked $godot @('--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','18000','--','--gameplay-capture') 'gameplay-rendered'
    if (!(Get-Content (Join-Path $PSScriptRoot 'manifests/gameplay-validation-visual.json') -Raw | ConvertFrom-Json).passed) { throw 'B1 rendered checks incomplete' }
}
if (!$SkipRegressions) {
    # Revalidate existing assets with the original validators; never rebuild or expand A4 terrain.
    foreach ($script in @('validate_glb.py','validate_character_glb.py')) {
        Invoke-B1Checked $python @((Join-Path $PSScriptRoot "scripts/$script")) "gameplay-regression-$script"
    }
    foreach ($script in @('validate_roundtrip.py','validate_character_roundtrip.py','validate_korriban_models.py')) {
        Invoke-B1Checked $blender @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $PSScriptRoot "scripts/$script")) "gameplay-regression-$script"
    }
    Invoke-B1Checked $imagePython @((Join-Path $PSScriptRoot 'scripts/record_korriban.py'),'ste2-input') 'gameplay-regression-ste2-input'
    Invoke-B1Checked 'dotnet' @('build',(Join-Path $PSScriptRoot 'scripts/ste2_compat/Ste2Test.csproj'),'--nologo',
        '-p:BaseOutputPath=../../work/korriban/ste2-bin/','-p:BaseIntermediateOutputPath=../../work/korriban/ste2-obj/') 'gameplay-regression-ste2-build'
    Invoke-B1Checked 'dotnet' @((Join-Path $PSScriptRoot 'work/korriban/ste2-bin/Debug/net8.0-windows/Ste2Test.dll'),
        (Join-Path $PSScriptRoot 'work/korriban/ste2-input'),(Join-Path $PSScriptRoot 'work/korriban/ste2-output')) 'gameplay-regression-ste2-run'
    Invoke-B1Checked $imagePython @((Join-Path $PSScriptRoot 'scripts/record_korriban.py'),'ste2-compare') 'gameplay-regression-ste2-compare'
    Invoke-B1Checked $godot @('--headless','--path',$project,'res://Inspection.tscn','--','--validate') 'gameplay-regression-door'
    Invoke-B1Checked $godot @('--headless','--path',$project,'res://CharacterInspection.tscn','--','--character-validate') 'gameplay-regression-character'
    Invoke-B1Checked $godot @('--headless','--path',$project,'res://LightningInspection.tscn','--','--lightning-validate') 'gameplay-regression-lightning'
    Invoke-B1Checked $godot @('--headless','--path',$project,'res://KorribanInspection.tscn','--','--korriban-validate') 'gameplay-regression-korriban'
    if ($Capture) {
        Invoke-B1Checked $godot @('--path',$project,'res://LightningInspection.tscn','--','--lightning-capture') 'gameplay-regression-lightning-rendered'
        Invoke-B1Checked $python @((Join-Path $PSScriptRoot 'scripts/validate_lightning_capture.py')) 'gameplay-regression-lightning-audio'
        Invoke-B1Checked $godot @('--path',$project,'res://KorribanInspection.tscn','--','--korriban-capture') 'gameplay-regression-korriban-rendered'
    }
    Invoke-B1Checked $python @((Join-Path $PSScriptRoot 'scripts/character_source.py'),'verify') 'gameplay-regression-character-source'
    Invoke-B1Checked $python @((Join-Path $PSScriptRoot 'scripts/record_lightning.py'),'verify') 'gameplay-regression-lightning-source'
    Invoke-B1Checked $imagePython @((Join-Path $PSScriptRoot 'scripts/record_korriban.py'),'verify') 'gameplay-regression-korriban-source'
    if (!$SkipArchiveRehash) {
        Invoke-B1Checked $python @((Join-Path $PSScriptRoot 'scripts/source_pipeline.py'),'verify') 'gameplay-regression-archives'
    }
}
if (Test-Path (Join-Path $PSScriptRoot 'manifests/gameplay-preservation-before.json')) {
    Invoke-B1Checked $python @((Join-Path $PSScriptRoot 'scripts/record_gameplay.py'),'after') 'gameplay-preservation'
}
Invoke-B1Checked $python @((Join-Path $PSScriptRoot 'scripts/record_gameplay.py'),'artifacts') 'gameplay-artifacts'
[ordered]@{ passed=$true; capturesThisRun=[bool]$Capture; regressionsThisRun=!$SkipRegressions;
    archiveRehashThisRun=(!$SkipRegressions -and !$SkipArchiveRehash);
    regressionScope='Original A1-A4 structural, geometry, animation, material, source and runtime validators; existing assets reused without regeneration.' } |
    ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'manifests/gameplay-run.json')
Write-Host 'B1 checks passed. Stop before combat.'
