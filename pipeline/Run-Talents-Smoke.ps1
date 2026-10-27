param([switch]$SkipImport)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'T1 build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/t1-talents-import.log')
    if($LASTEXITCODE -ne 0){throw 'T1 import failed'}
}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/t1-talents-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    & $godot --path $project --rendering-method gl_compatibility --resolution 1600x900 --max-fps 60 --quit-after 1500 -- --talents-smoke *> (Join-Path $PSScriptRoot 'logs/t1-talents-smoke.log')
    if($LASTEXITCODE -ne 0){throw 'T1 runtime smoke failed'}
    & $godot --headless --path $project --quit-after 100 -- --talents-persistence-check *> (Join-Path $PSScriptRoot 'logs/t1-talents-persistence.log')
    if($LASTEXITCODE -ne 0){throw 'T1 persistence check failed'}
    foreach($stage in @('smoke','persistence')){
        $report=Get-Content (Join-Path $PSScriptRoot "manifests/t1-talents-$stage.json") -Raw | ConvertFrom-Json
        if(!$report.passed){throw $report.failure}
        if((Get-Content (Join-Path $PSScriptRoot "logs/t1-talents-$stage.log") -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "T1 $stage runtime errors"}
        Write-Host "$stage passed $($report.checks.Count) checks"
    }
} finally {$env:APPDATA=$previousAppData}
