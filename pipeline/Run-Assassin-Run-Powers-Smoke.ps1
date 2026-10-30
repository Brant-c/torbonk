$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Assassin run powers build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot ('work/assassin-run-powers-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $log=Join-Path $PSScriptRoot 'logs/assassin-run-powers-smoke.log'
    $receipt=Join-Path $PSScriptRoot 'manifests/assassin-run-powers-smoke.json'
    if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1500 -- --advanced-class=assassin --assassin-run-powers-smoke *> $log
    if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $receipt)){throw "Assassin run powers failed; see $log"}
    $report=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    if(!$report.passed -or (Get-Content $log -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "Assassin run powers failed: $($report.failure)"}
    Write-Host "Assassin run powers passed $($report.checks.Count) checks."
}finally {$env:APPDATA=$previousAppData}
