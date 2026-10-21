param([switch]$Capture, [switch]$SkipRegressions, [switch]$SkipArchiveRehash)
$ErrorActionPreference = 'Stop'
$pipelineRoot = $PSScriptRoot
$python = 'C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$imagePython = 'C:\Users\brant\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$godot = Join-Path $pipelineRoot 'tools\godot\Godot_console.exe'
$project = Join-Path $pipelineRoot 'godot'
function Invoke-LightningChecked([string]$Program, [string[]]$Arguments, [string]$Name) {
    $logPath = Join-Path $pipelineRoot "logs\$Name.log"
    & $Program @Arguments *> $logPath
    if ($LASTEXITCODE -ne 0) { throw "$Name failed ($LASTEXITCODE); see $logPath" }
    if ((Get-Content -LiteralPath $logPath -Raw) -match 'SCRIPT ERROR:|Parse Error:') { throw "$Name had script errors; see $logPath" }
    if ($Name -in @('lightning-validation','lightning-visual-validation') -and (Get-Content -LiteralPath $logPath -Raw) -notmatch 'LIGHTNING_VALIDATION_PASSED') { throw "$Name did not complete its checks" }
}
& (Join-Path $pipelineRoot 'Assert-Toolchain.ps1')
$beforePath = Join-Path $pipelineRoot 'manifests\lightning-artifacts.json'
$before = if (Test-Path $beforePath) { Get-Content $beforePath -Raw | ConvertFrom-Json } else { $null }
Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\record_lightning.py'),'tools') 'lightning-toolchain'
Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\probe_lightning_audio.py')) 'lightning-audio-discovery'
Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\lightning_source.py')) 'lightning-source'
$wwiser = Join-Path $pipelineRoot 'tools\wwiser.pyz'
$dataBank = Join-Path $pipelineRoot 'source-assets\april2012\resources\bnk2\abilities_imperialmelee_data.bnk'
$mediaBank = Join-Path $pipelineRoot 'source-assets\april2012\resources\bnk2\abilities_imperialmelee_media.bnk'
$initBank = Join-Path $pipelineRoot 'source-assets\april2012\resources\bnk2\init.bnk'
Invoke-LightningChecked $python @($wwiser,$dataBank,'-d','xml','-dn',(Join-Path $pipelineRoot 'work\lightning\bank')) 'lightning-bank-parse'
Invoke-LightningChecked $python @($wwiser,$dataBank,$mediaBank,$initBank,'-d','none','-g','-gf','2909241004','1354101915','846159110','-go',(Join-Path $pipelineRoot 'work\lightning\events'),'-gw','.') 'lightning-bank-events'
Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\convert_lightning_audio.py')) 'lightning-audio-conversion'
Invoke-LightningChecked $imagePython @((Join-Path $pipelineRoot 'scripts\convert_lightning_visuals.py')) 'lightning-visual-conversion'
$runtime = Join-Path $pipelineRoot 'logs\lightning-runtime.log'
Invoke-LightningChecked $godot @('--headless','--editor','--path',$project,'--import','--log-file',$runtime) 'lightning-import'
Invoke-LightningChecked $godot @('--headless','--path',$project,'res://LightningInspection.tscn','--quit-after','3600','--log-file',$runtime,'--','--lightning-validate') 'lightning-validation'
if (!(Get-Content (Join-Path $pipelineRoot 'manifests\lightning-godot-validation.json') -Raw | ConvertFrom-Json).passed) { throw 'Lightning validation did not pass' }
if ($Capture) {
    Invoke-LightningChecked $godot @('--path',$project,'res://LightningInspection.tscn','--quit-after','3600','--log-file',$runtime,'--','--lightning-capture') 'lightning-visual-validation'
    if (!(Get-Content (Join-Path $pipelineRoot 'manifests\lightning-godot-validation-visual.json') -Raw | ConvertFrom-Json).passed) { throw 'Rendered validation did not pass' }
    Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\validate_lightning_capture.py')) 'lightning-runtime-audio-validation'
}
if (!$SkipRegressions) {
    & (Join-Path $pipelineRoot 'Run-Character-Pipeline.ps1') -SkipArchiveRehash:$SkipArchiveRehash
}
Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\record_lightning.py'),'verify') 'lightning-source-integrity'
Invoke-LightningChecked $python @((Join-Path $pipelineRoot 'scripts\record_lightning.py'),'artifacts') 'lightning-artifact-hashes'
$after = Get-Content $beforePath -Raw | ConvertFrom-Json
$changed = @()
if ($null -ne $before) {
    foreach ($asset in $after.convertedAssets) {
        $previous = $before.convertedAssets | Where-Object path -eq $asset.path
        if ($null -ne $previous -and $previous.sha256 -ne $asset.sha256) { $changed += $asset.path }
    }
}
[ordered]@{ passed = ($changed.Count -eq 0); changedConvertedAssets = $changed; comparedPrevious = ($null -ne $before); regressionThisRun = !$SkipRegressions; archiveRehashThisRun = (!$SkipRegressions -and !$SkipArchiveRehash); capturesThisRun = [bool]$Capture } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $pipelineRoot 'manifests\lightning-repeat-validation.json')
if ($changed.Count) { throw 'Converted audio/textures changed on repeat; inspect lightning-repeat-validation.json' }
Write-Host 'Milestone A3 complete. Stop before gameplay or environment reconstruction.'
