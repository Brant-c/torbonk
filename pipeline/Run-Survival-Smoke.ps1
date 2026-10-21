param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B3 C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/survival-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$runtimeLog = Join-Path $PSScriptRoot 'logs/survival-smoke-runtime.log'
# One bounded rendered run, with no conversion, screenshots or regression runners.
& (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file $runtimeLog --quit-after 3600 -- --survival-smoke
if ($LASTEXITCODE -ne 0) { throw 'B3 runtime smoke failed; see logs/survival-smoke-runtime.log' }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B3 smoke incomplete or failed' }
$problems = Get-Content -LiteralPath $runtimeLog | Where-Object {
    $_ -match 'SCRIPT ERROR:|Parse Error:|ERROR:' -and $_ -notmatch "^ERROR: Can't create shader cache folder, no shader caching will happen: user://$"
}
if ($problems) { throw 'B3 runtime reported errors; see logs/survival-smoke-runtime.log' }
Write-Host 'B3 smoke passed. Ready for manual gameplay review.'
