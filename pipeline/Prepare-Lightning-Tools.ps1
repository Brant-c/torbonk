$ErrorActionPreference = 'Stop'
$toolsRoot = Join-Path $PSScriptRoot 'tools'
$packages = @(
    @{ File='wwiser.pyz'; Url='https://github.com/bnnm/wwiser/releases/download/v20260808/wwiser.pyz'; Hash='f4ba1368895adab285f27fa37b142e5da805bcc6fb77e8daba282dac65d89411' },
    @{ File='vgmstream-win64.zip'; Url='https://github.com/vgmstream/vgmstream/releases/download/r2117/vgmstream-win64.zip'; Hash='6c4a8a3813864fefed081bbd337dbc0ad93bf88e0b92f5db98d7ab258b22dc6c' }
)
foreach ($package in $packages) {
    $output = Join-Path $toolsRoot $package.File
    if (!(Test-Path -LiteralPath $output)) { Invoke-WebRequest $package.Url -OutFile $output }
    if ((Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant() -ne $package.Hash) { throw "Wrong package hash: $output" }
}
if (!(Test-Path -LiteralPath (Join-Path $toolsRoot 'vgmstream\vgmstream-cli.exe'))) {
    Expand-Archive -LiteralPath (Join-Path $toolsRoot 'vgmstream-win64.zip') -DestinationPath (Join-Path $toolsRoot 'vgmstream')
}
Write-Host 'Pinned A3 audio tools are ready. No machine-wide installation was performed.'
