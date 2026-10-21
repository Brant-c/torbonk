param()
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo
if($LASTEXITCODE -ne 0){throw 'D1 C# build failed'}
$receipt=Join-Path $PSScriptRoot 'manifests/d1-smoke.json'
if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
$previousAppData=$env:APPDATA
$smokeAppData=Join-Path $PSScriptRoot 'work/d1-userdata'
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA=$smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/d1-import.log') *> (Join-Path $PSScriptRoot 'logs/d1-import-console.log')
    if($LASTEXITCODE -ne 0){throw 'D1 changed-resource import failed'}
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --path $project res://Gameplay/GameWorld.tscn --log-file (Join-Path $PSScriptRoot 'logs/d1-runtime.log') --max-fps 60 --quit-after 900 -- --d1-smoke *> (Join-Path $PSScriptRoot 'logs/d1-runtime-console.log')
    if($LASTEXITCODE -ne 0){throw 'D1 runtime smoke failed; inspect logs/d1-runtime-console.log'}
} finally {$env:APPDATA=$previousAppData}
if(!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed){throw 'D1 smoke incomplete'}
foreach($log in @('d1-runtime-console.log','d1-import-console.log')) {
    if((Get-Content (Join-Path $PSScriptRoot ('logs/'+$log)) -Raw) -match 'SCRIPT ERROR:|Parse Error:|ERROR:'){throw ('D1 engine errors in '+$log)}
}
Write-Host 'D1 compile and brief runtime passed. Stop for manual visual review.'
