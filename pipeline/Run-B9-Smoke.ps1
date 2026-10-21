param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'B9 C# build failed' }
$receipt = Join-Path $PSScriptRoot 'manifests/b9-smoke.json'
if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt }
$previousAppData = $env:APPDATA
$smokeAppData = Join-Path $PSScriptRoot 'work/b9-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA = $smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/b9-import.log') *> (Join-Path $PSScriptRoot 'logs/b9-import-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'B9 changed-resource import failed' }
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project --log-file (Join-Path $PSScriptRoot 'logs/b9-runtime.log') --max-fps 60 --quit-after 1500 -- --b9-smoke *> (Join-Path $PSScriptRoot 'logs/b9-runtime-console.log')
    if ($LASTEXITCODE -ne 0) { throw 'B9 runtime smoke failed' }
} finally { $env:APPDATA = $previousAppData }
if (!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed) { throw 'B9 smoke incomplete or failed' }
foreach ($log in @('b9-runtime.log', 'b9-runtime-console.log', 'b9-import-console.log')) {
    if ((Get-Content (Join-Path $PSScriptRoot ('logs/' + $log)) -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:') { throw ('B9 reported errors in ' + $log) }
}
Write-Host 'B9 smoke passed. Stop for manual cursor/camera review.'
