param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'C2 C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/c2-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/c2-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/c2-import.log') *> (Join-Path $PSScriptRoot 'logs/c2-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'C2 frontend import failed' }
    if ((Get-Content (Join-Path $PSScriptRoot 'logs/c2-import-console.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'C2 import reported errors' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project --log-file (Join-Path $PSScriptRoot 'logs/c2-runtime.log') --max-fps 60 --quit-after 2400 -- --c2-smoke
    if ($LASTEXITCODE -ne 0) { throw 'C2 runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'C2 smoke incomplete or failed' }
if ((Get-Content (Join-Path $PSScriptRoot 'logs/c2-runtime.log') -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw 'C2 runtime reported errors' }
Write-Host 'C2 integration smoke passed. Stop here for manual review.'
