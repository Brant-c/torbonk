param([switch]$SkipImport,[switch]$Review)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'T2.2 build failed'}
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/t22-import.log')
    if($LASTEXITCODE -ne 0){throw 'T2.2 import failed'}
}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/t22-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $receipt=Join-Path $PSScriptRoot 'manifests/t22-sorcerer-smoke.json'
    if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
    $argsList=@('--headless','--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','1000','--','--sorcerer-refinement-smoke','--advanced-class=sorcerer')
    if($Review){$argsList=@('--path',$project,'res://Gameplay/GameWorld.tscn','--rendering-method','gl_compatibility','--resolution','1600x900','--max-fps','60','--quit-after','1000','--','--sorcerer-refinement-smoke','--advanced-class=sorcerer','--t22-review')}
    & $godot @argsList *> (Join-Path $PSScriptRoot 'logs/t22-smoke.log')
    if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $receipt)){throw 'T2.2 smoke did not complete'}
    $result=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    if(!$result.passed -or (Get-Content (Join-Path $PSScriptRoot 'logs/t22-smoke.log') -Raw) -match 'ERROR:|SCRIPT ERROR:'){throw "T2.2 failed: $($result.failure)"}
    Write-Host "T2.2 passed $($result.checks.Count) focused checks."
} finally {$env:APPDATA=$previousAppData}
