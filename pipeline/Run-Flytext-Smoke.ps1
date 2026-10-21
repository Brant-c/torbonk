param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Flytext C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/flytext-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$runtimeLog = Join-Path $PSScriptRoot 'logs/flytext-smoke-runtime.log'
# Keep this opt-in smoke's engine cache inside the workspace, including under Codex's sandbox.
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/flytext-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file $runtimeLog --quit-after 900 -- --flytext-smoke
} finally { $env:APPDATA = $previousAppData }
if ($LASTEXITCODE -ne 0) { throw 'Flytext runtime smoke failed; see logs/flytext-smoke-runtime.log' }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'Flytext smoke incomplete or failed' }
if ((Get-Content -LiteralPath $runtimeLog -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'Flytext runtime reported errors; see log' }
Write-Host 'Flytext smoke passed. Stop here for manual visual review.'
