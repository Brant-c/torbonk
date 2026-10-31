param([ValidateSet('juggernaut','marauder','both')][string]$Class='both')
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Warrior talents build failed'}
$previousAppData=$env:APPDATA
try {
    foreach($class in $(if($Class -eq 'both'){'juggernaut','marauder'}else{$Class})) {
        $env:APPDATA=Join-Path $PSScriptRoot ("work/warrior-talents-$class-"+[guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
        $log=Join-Path $PSScriptRoot "logs/warrior-talents-$class-smoke.log"
        $receipt=Join-Path $PSScriptRoot "manifests/warrior-talents-$class-smoke.json"
        if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
        & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --path $project res://Gameplay/GameWorld.tscn --quit-after 2500 -- --advanced-class=$class --warrior-talents-smoke *> $log
        if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $receipt)){throw "$class warrior talents failed; see $log"}
        $report=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
        if(!$report.passed -or (Get-Content $log -Raw) -match '(?m)^ERROR:|SCRIPT ERROR:'){throw "$class warrior talents failed: $($report.failure)"}
        Write-Host "$class warrior talents passed $($report.checks.Count) checks."
    }
}finally {$env:APPDATA=$previousAppData}
