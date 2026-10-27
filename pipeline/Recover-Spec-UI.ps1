param([string]$Python='C:/Users/brant/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
$ErrorActionPreference='Stop'
& $Python (Join-Path $PSScriptRoot 'scripts/prepare_spec_ui.py') index
if($LASTEXITCODE -ne 0){throw 'Spec artwork lookup failed'}
Add-Type -Path D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/ZstdSharp.dll
$decoder=[ZstdSharp.Decompressor]::new()
try {
    $receipt=Get-Content (Join-Path $PSScriptRoot 'manifests/spec-ui-installed-source.json') -Raw | ConvertFrom-Json
    foreach($row in $receipt.resources){
        $packed=[IO.File]::ReadAllBytes($row.packedPath)
        if($row.compression -eq 1){
            $decoded=[byte[]]::new($row.length)
            $count=$decoder.Unwrap($packed,$decoded,0)
            if($count -ne $row.length){throw 'Decoded icon length mismatch'}
        } else {$decoded=$packed}
        [IO.File]::WriteAllBytes($row.sourcePath,$decoded)
    }
} finally {$decoder.Dispose()}
& $Python (Join-Path $PSScriptRoot 'scripts/prepare_spec_ui.py') convert
if($LASTEXITCODE -ne 0){throw 'Spec artwork conversion failed'}
