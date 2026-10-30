$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Agent build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot ('work/agent-smoke-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    & $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/agent-import.log')
    if($LASTEXITCODE -ne 0){throw 'Agent import failed'}
    foreach($class in @('operative','sniper')){
        $log=Join-Path $PSScriptRoot "logs/agent-$class-smoke.log"
        & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 800 -- --agent-expansion-smoke "--advanced-class=$class" *> $log
        if($LASTEXITCODE -ne 0){throw "Agent $class failed"}
        $report=Get-Content (Join-Path $PSScriptRoot "manifests/agent-$class-smoke.json") -Raw | ConvertFrom-Json
        if(!$report.passed -or (Get-Content $log -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "Agent $class failed: $($report.failure)"}
        Write-Host "$class passed $($report.checks.Count) checks."
        $presentationLog=Join-Path $PSScriptRoot "logs/agent-$class-presentation.log"
        & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 900 -- --agent-presentation-smoke "--advanced-class=$class" *> $presentationLog
        if($LASTEXITCODE -ne 0){throw "Agent $class presentation failed"}
        $presentation=Get-Content (Join-Path $PSScriptRoot "manifests/agent-$class-presentation.json") -Raw | ConvertFrom-Json
        if(!$presentation.passed -or (Get-Content $presentationLog -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "Agent $class presentation failed: $($presentation.failure)"}
        Write-Host "$class presentation passed $($presentation.checks.Count) checks."
    }
    $coverLog=Join-Path $PSScriptRoot 'logs/sniper-cover-smoke.log'
    & $godot --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 1800 -- --sniper-cover-smoke --advanced-class=sniper *> $coverLog
    if($LASTEXITCODE -ne 0){throw 'Sniper cover failed'}
    $cover=Get-Content (Join-Path $PSScriptRoot 'manifests/sniper-cover-smoke.json') -Raw | ConvertFrom-Json
    if(!$cover.passed -or (Get-Content $coverLog -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "Sniper cover failed: $($cover.failure)"}
    Write-Host "Sniper cover passed $($cover.checks.Count) checks."
} finally {$env:APPDATA=$previousAppData}
