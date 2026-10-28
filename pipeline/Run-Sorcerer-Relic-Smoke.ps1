$ErrorActionPreference='Stop'
& dotnet build (Join-Path $PSScriptRoot 'godot/SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/relic-userdata'
    $receipt=Join-Path $PSScriptRoot 'manifests/sorcerer-relic-smoke.json'
    if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
    $log=Join-Path $PSScriptRoot 'logs/sorcerer-relic-smoke.log'
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --path (Join-Path $PSScriptRoot 'godot') res://Gameplay/GameWorld.tscn --quit-after 1000 -- --sorcerer-relic-smoke --advanced-class=sorcerer *> $log
    if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $receipt)){throw 'Relic smoke did not complete'}
    $result=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    if(!$result.passed -or (Get-Content -LiteralPath $log -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw "Relic smoke failed: $($result.failure)"}
    Write-Host "Sorcerer relics passed $($result.checks.Count) checks."
} finally {$env:APPDATA=$previousAppData}
