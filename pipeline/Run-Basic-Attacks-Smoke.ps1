param([switch]$SkipImport)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Basic attack build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/basic-attacks-import.log')
    if($LASTEXITCODE -ne 0){throw 'Basic attack import failed'}
}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/basic-attacks-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach($class in @('sorcerer','assassin','juggernaut','marauder','operative','sniper','mercenary','powertech')){
        $receipt=Join-Path $PSScriptRoot "manifests/basic-attacks-$class-smoke.json"
        if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
        $log=Join-Path $PSScriptRoot "logs/basic-attacks-$class-console.log"
        & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1000 --log-file (Join-Path $PSScriptRoot "logs/basic-attacks-$class-runtime.log") -- --basic-attacks-smoke "--advanced-class=$class" *> $log
        if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $receipt)){throw "$class smoke did not complete"}
        $result=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
        if(!$result.passed -or (Get-Content -LiteralPath $log -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw "$class smoke failed: $($result.failure)"}
        Write-Host "$class passed $($result.checks.Count) focused checks."
    }
} finally {$env:APPDATA=$previousAppData}
