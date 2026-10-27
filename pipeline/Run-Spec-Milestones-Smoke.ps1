param([switch]$SkipImport,[switch]$Review)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Spec milestone build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --quit *> (Join-Path $PSScriptRoot 'logs/spec-milestones-import.log')
    if($LASTEXITCODE -ne 0){throw 'Spec milestone import failed'}
}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/spec-milestones-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach($class in @('sorcerer','assassin')){
        $receipt=Join-Path $PSScriptRoot "manifests/spec-milestones-$class-smoke.json"
        if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
        $log=Join-Path $PSScriptRoot "logs/spec-$class-console.log"
        $arguments=@('--path',$project,'res://Gameplay/GameWorld.tscn','--quit-after','5000','--log-file',(Join-Path $PSScriptRoot "logs/spec-$class-runtime.log"))
        if($Review){$arguments+=@('--rendering-method','gl_compatibility','--max-fps','60')}else{$arguments+='--headless'}
        $arguments+=@('--','--spec-milestones-smoke',"--advanced-class=$class")
        if($Review){$arguments+='--spec-milestones-review'}
        & $godot @arguments *> $log
        if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $receipt)){throw "$class milestone checks did not complete"}
        $result=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
        if(!$result.passed -or (Get-Content -LiteralPath $log -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw "$class milestone checks failed: $($result.failure)"}
        Write-Host "$class passed $($result.checks.Count) checks."
    }
} finally {$env:APPDATA=$previousAppData}
