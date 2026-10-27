param([switch]$SkipImport,[switch]$Review)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Assassin refinement build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot ('work/assassin-refinement-import-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/assassin-refinement-import.log');if($LASTEXITCODE -ne 0){throw 'Assassin import failed'}}
    foreach($build in @('baseline','madness','deception')){
        $env:APPDATA=Join-Path $PSScriptRoot ('work/assassin-refinement-'+$build+'-'+[guid]::NewGuid().ToString('N'))
        $profileDir=Join-Path $env:APPDATA 'Godot/app_userdata/SWTOR Survivors - Static Asset Inspection'
        New-Item -ItemType Directory -Force -Path $profileDir | Out-Null
        $ranks=@{}
        if($build -eq 'madness'){$ranks=@{'assassin_madness.creeping_terror'=1;'assassin_madness.parasitism'=1}}
        if($build -eq 'deception'){$ranks=@{'deception.thrashing_blades'=1;'deception.duplicity'=1}}
        @{SchemaVersion=1;Profiles=@{assassin=$ranks}} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $profileDir 'talent-profiles.json')
        $log=Join-Path $PSScriptRoot "logs/assassin-refinement-$build.log"
        $argsList=@('--headless','--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','1200','--','--assassin-refinement-smoke','--advanced-class=assassin',"--assassin-build=$build")
        if($Review -and $build -eq 'baseline'){$argsList=@('--path',$project,'res://Gameplay/GameWorld.tscn','--rendering-method','gl_compatibility','--resolution','1600x900','--max-fps','60','--quit-after','1200','--','--assassin-refinement-smoke','--advanced-class=assassin','--assassin-review')}
        & $godot @argsList *> $log
        if($LASTEXITCODE -ne 0){throw "$build Assassin refinement failed"}
        $report=Get-Content (Join-Path $PSScriptRoot "manifests/assassin-refinement-$build-smoke.json") -Raw | ConvertFrom-Json
        if(!$report.passed -or (Get-Content $log -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "$build failed: $($report.failure)"}
        Write-Host "$build passed $($report.checks.Count) checks."
    }
}finally {$env:APPDATA=$previousAppData}
