param([string[]]$Classes=@('marauder','operative','powertech','sniper'),[switch]$SkipImport)
$ErrorActionPreference='Stop'
$Classes=@($Classes | ForEach-Object { $_ -split ',' })
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Kit C# build failed'}
$godot=Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe'
if(!$SkipImport){
    & $godot --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/kits-import.log') *> (Join-Path $PSScriptRoot 'logs/kits-import-console.log')
    if($LASTEXITCODE -ne 0){throw 'Kit import failed'}
}
$previousAppData=$env:APPDATA
try {
    foreach($class in $Classes){
        $env:APPDATA=Join-Path $PSScriptRoot 'work/kits-userdata'
        New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
        & $godot --path $project res://Gameplay/GameWorld.tscn --max-fps 60 --quit-after 1800 --log-file (Join-Path $PSScriptRoot "logs/kits-$class.log") -- --kit-smoke "--advanced-class=$class" *> (Join-Path $PSScriptRoot "logs/kits-$class-console.log")
        $receipt=Get-Content (Join-Path $PSScriptRoot "manifests/kit-smoke-$class.json") -Raw | ConvertFrom-Json
        if(!$receipt.passed){throw "Kit checks failed for $class : $($receipt.failure)"}
        if((Get-Content (Join-Path $PSScriptRoot "logs/kits-$class-console.log") -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw "Kit runtime errors for $class"}
        Write-Host "$class kit checks passed ($($receipt.checks.Count))."
    }
} finally {$env:APPDATA=$previousAppData}
