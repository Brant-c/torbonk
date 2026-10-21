$ErrorActionPreference = 'Stop'
$toolLock = Get-Content (Join-Path $PSScriptRoot 'manifests\tooling.lock.json') -Raw | ConvertFrom-Json
foreach ($package in $toolLock.packages) {
    $path = Join-Path $PSScriptRoot "tools\packages\$($package.name)"
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $package.sha256) {
        throw "Pinned package changed: $path"
    }
}
foreach ($tool in $toolLock.toolFiles) {
    $path = Join-Path $PSScriptRoot $tool.path
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $tool.sha256) {
        throw "Pinned add-on/runtime file changed: $path"
    }
}
foreach ($tool in $toolLock.executables) {
    if ((Get-FileHash -LiteralPath $tool.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $tool.sha256) {
        throw "Pinned executable changed: $($tool.path)"
    }
}
