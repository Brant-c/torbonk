# Restore only the pinned packages and a private copy of the already installed engine.
param([string]$InstalledGodotRoot = 'D:\WorkspaceVS')
$ErrorActionPreference = 'Stop'
$pipelineRoot = $PSScriptRoot
$toolLock = Get-Content (Join-Path $pipelineRoot 'manifests\tooling.lock.json') -Raw | ConvertFrom-Json
foreach ($package in $toolLock.packages) {
    $destination = Join-Path $pipelineRoot "tools\packages\$($package.name)"
    if (!(Test-Path $destination)) {
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Invoke-WebRequest $package.url -OutFile $destination
    }
    if ((Get-FileHash $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $package.sha256) { throw "Package hash mismatch: $destination" }
    $module = if ($package.name -eq 'io_scene_gr2.zip') { 'io_scene_gr2' } else { 'zg_swtor_tools' }
    if (!(Test-Path (Join-Path $pipelineRoot "tools\addons\$module\__init__.py"))) {
        Expand-Archive -LiteralPath $destination -DestinationPath (Join-Path $pipelineRoot 'tools\addons')
    }
}
$engineRoot = Join-Path $pipelineRoot 'tools\godot'
New-Item -ItemType Directory -Path $engineRoot -Force | Out-Null
foreach ($mapping in @(
    @{ Source='Godot_v4.7.2-stable_mono_win64.exe'; Target='Godot.exe' },
    @{ Source='Godot_v4.7.2-stable_mono_win64_console.exe'; Target='Godot_console.exe' }
)) {
    $targetPath = Join-Path $engineRoot $mapping.Target
    if (!(Test-Path $targetPath)) { Copy-Item -LiteralPath (Join-Path $InstalledGodotRoot $mapping.Source) -Destination $targetPath }
}
if (!(Test-Path (Join-Path $engineRoot 'GodotSharp'))) { Copy-Item -LiteralPath (Join-Path $InstalledGodotRoot 'GodotSharp') -Destination $engineRoot -Recurse }
if (!(Test-Path (Join-Path $engineRoot '_sc_'))) { New-Item -ItemType File -Path (Join-Path $engineRoot '_sc_') | Out-Null }
Write-Host 'Pinned tools prepared. Run-Pipeline.ps1 checks every add-on and runtime hash.'
