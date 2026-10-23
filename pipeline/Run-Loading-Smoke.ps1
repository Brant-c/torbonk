param([ValidateSet('korriban','hutta','dromund','balmorra')][string[]]$Map = @('korriban','hutta','dromund','balmorra'))
$ErrorActionPreference = 'Stop'
& dotnet build "$PSScriptRoot/godot/SwtorAssetInspection.csproj" --no-restore --nologo -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/loading-user'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach ($destination in $Map) {
        $log=Join-Path $PSScriptRoot "logs/loading-$destination.log"
        & "$PSScriptRoot/tools/godot/Godot_console.exe" --path "$PSScriptRoot/godot" res://Integration/LoadingScreenSmoke.tscn -- "--map=$destination" *> $log
        if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch "LOADING_SMOKE $destination PASS")) { throw "Loading smoke failed: $destination; see $log" }
        Write-Host "Loading transition passed: $destination"
    }
} finally { $env:APPDATA=$previousAppData }
