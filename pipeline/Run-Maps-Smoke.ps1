param([ValidateSet('hutta','dromund','balmorra','korriban')][string[]]$Map = @('hutta','dromund','balmorra'))
# Runs the existing B3 survival smoke (spawn, chase, damage, waves, death) on each map section.
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'C# build failed' }
$worlds = @{ korriban = 'GameWorld'; hutta = 'GameWorld_Hutta'; dromund = 'GameWorld_Dromund'; balmorra = 'GameWorld_Balmorra' }
foreach ($m in $Map) {
    $receipt = Join-Path $PSScriptRoot 'manifests/survival-smoke.json'
    if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
    $log = Join-Path $PSScriptRoot "logs/map-$m-smoke-runtime.log"
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project "res://Gameplay/$($worlds[$m]).tscn" --log-file $log --quit-after 3600 -- --advanced-class=sorcerer "--map=$m" --survival-smoke *> (Join-Path $PSScriptRoot "logs/map-$m-smoke-console.log")
    $code = $LASTEXITCODE; $ErrorActionPreference = $previous
    if ($code -ne 0) { throw "$m smoke runtime failed ($code); see $log" }
    if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw "$m smoke incomplete or failed" }
    Copy-Item -LiteralPath $receipt -Destination (Join-Path $PSScriptRoot "manifests/map-$m-smoke.json") -Force
    Write-Host "$m survival smoke passed."
}
