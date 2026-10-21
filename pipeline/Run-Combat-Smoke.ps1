param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B2 C# build failed' }
# One real-renderer run; no import, conversion, screenshots, archive scan or regression pipeline.
$receipt = Join-Path $PSScriptRoot 'manifests/combat-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
& (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/combat-smoke-runtime.log') --quit-after 7200 -- --combat-smoke
if ($LASTEXITCODE -ne 0) { throw 'B2 rendered smoke failed; see logs/combat-smoke-runtime.log' }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B2 smoke incomplete or failed' }
$problems = Get-Content (Join-Path $PSScriptRoot 'logs/combat-smoke-runtime.log') | Where-Object {
    $_ -match 'SCRIPT ERROR:|Parse Error:|ERROR:' -and $_ -notmatch "^ERROR: Can't create shader cache folder, no shader caching will happen: user://$"
}
if ($problems) { throw 'B2 runtime reported errors; see logs/combat-smoke-runtime.log' }
Write-Host 'B2 focused rendered smoke passed. Gameplay is ready for manual testing.'
