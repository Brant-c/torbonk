param([switch]$SkipImport,[switch]$Capture)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Assassin presentation build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot ('work/assassin-presentation-'+[guid]::NewGuid().ToString('N'))
    $profileDir=Join-Path $env:APPDATA 'Godot/app_userdata/SWTOR Survivors - Static Asset Inspection'
    New-Item -ItemType Directory -Force -Path $profileDir|Out-Null
    $ranks=@{}
    foreach($id in @('darkness.dark_ward','darkness.lightning_reflexes','darkness.shroud_of_darkness','darkness.premonition','darkness.hollow','deception.thrashing_blades','deception.induction','deception.shadow_training','deception.saber_conduit','deception.dark_swell','deception.chain_shock')){$ranks[$id]=1}
    @{SchemaVersion=1;Profiles=@{assassin=$ranks}}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $profileDir 'talent-profiles.json')
    if(!$SkipImport){& $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/assassin-presentation-import.log');if($LASTEXITCODE -ne 0){throw 'Import failed'}}
    $argsList=@('--headless','--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','2500','--','--assassin-presentation-smoke','--advanced-class=assassin')
    $log=Join-Path $PSScriptRoot 'logs/assassin-presentation-smoke.log'
    $receipt=Join-Path $PSScriptRoot 'manifests/assassin-presentation-smoke.json'
    if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
    if($Capture){
        $argsList=@('--path',$project,'res://Gameplay/GameWorld.tscn','--rendering-method','gl_compatibility','--resolution','1600x900','--max-fps','60','--quit-after','2500','--','--assassin-presentation-smoke','--advanced-class=assassin','--assassin-presentation-captures')
        $process=Start-Process -FilePath $godot -ArgumentList $argsList -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $log -RedirectStandardError (Join-Path $PSScriptRoot 'logs/assassin-presentation-render-errors.log')
        if($process.ExitCode -ne 0){throw 'Rendered Assassin checks failed'}
    } else {& $godot @argsList *> $log;if($LASTEXITCODE -ne 0){throw 'Assassin presentation checks failed'}}
    if(!(Test-Path -LiteralPath $receipt)){throw 'Assassin checks did not produce a result'}
    if($Capture -and (Get-Content (Join-Path $PSScriptRoot 'logs/assassin-presentation-render-errors.log') -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw 'Rendered Assassin checks logged an error'}
    $report=Get-Content -LiteralPath $receipt -Raw|ConvertFrom-Json
    if(!$report.passed -or (Get-Content $log -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "Assassin presentation failed: $($report.failure)"}
    Write-Host "Assassin presentation passed $($report.checks.Count) checks."
} finally {$env:APPDATA=$previousAppData}
