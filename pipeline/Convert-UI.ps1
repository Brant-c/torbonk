param([string]$Python = (Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'))
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Python)) { throw 'Specify -Python with a Python installation containing Pillow. fontTools 4.60.1 is vendored in pipeline/tools/python-ui.' }
& $Python (Join-Path $PSScriptRoot 'scripts/convert_ui.py')
if ($LASTEXITCODE -ne 0) { throw 'UI extraction/conversion failed' }
Write-Host 'Original bytes: source-assets/april2012. Generated art/fonts: godot/Assets/UI. Provenance: manifests/ui-source.json and ui-conversion.json.'
