param([switch]$HudReview, [switch]$LevelReview)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B10.1 C# build failed' }
$receiptName = if ($LevelReview) { 'b101-level-smoke.json' } else { 'b101-smoke.json' }
$receipt = Join-Path $PSScriptRoot ('manifests/' + $receiptName)
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/b101-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/b101-import.log') *> (Join-Path $PSScriptRoot 'logs/b101-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'B10.1 resource import failed' }
    $smokeFlags = @('--b101-smoke')
    if ($HudReview) { $smokeFlags += '--b101-hud-review' }
    if ($LevelReview) { $smokeFlags += '--b101-level-review' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project --log-file (Join-Path $PSScriptRoot 'logs/b101-runtime.log') --max-fps 60 --quit-after 2400 -- @smokeFlags *> (Join-Path $PSScriptRoot 'logs/b101-runtime-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'B10.1 runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B10.1 smoke incomplete or failed' }
foreach ($log in @('b101-runtime.log', 'b101-runtime-console.log', 'b101-import-console.log')) {
    if ((Get-Content (Join-Path $PSScriptRoot ('logs/' + $log)) -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw ('B10.1 reported errors in ' + $log) }
}
Write-Host 'B10.1 smoke passed. Stop for manual UI review.'
