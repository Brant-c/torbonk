param()
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Jump C# build failed' }
$receipt=Join-Path $PSScriptRoot 'manifests/b9-jump-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData=$env:APPDATA
$smokeAppData=Join-Path $PSScriptRoot 'work/b9-jump-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA=$smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/b9-jump-import.log') *> (Join-Path $PSScriptRoot 'logs/b9-jump-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'Jump resource import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/b9-jump-runtime.log') --max-fps 60 --quit-after 1200 -- --b9-jump-smoke *> (Join-Path $PSScriptRoot 'logs/b9-jump-runtime-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'Jump runtime smoke failed' }
} finally { $env:APPDATA=$previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'Jump smoke incomplete or failed' }
foreach ($log in @('b9-jump-runtime.log','b9-jump-runtime-console.log','b9-jump-import-console.log')) {
    if ((Get-Content (Join-Path $PSScriptRoot ('logs/'+$log)) -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw ('Jump errors in '+$log) }
}
Write-Host 'Original jump animation smoke passed. Stop for manual review.'
