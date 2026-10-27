param([switch]$Rendered,[ValidateSet('korriban','hutta')][string[]]$Planets=@('korriban','hutta'))
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Difficulty build failed'}
$previousAppData=$env:APPDATA
try {
    $env:APPDATA=Join-Path $PSScriptRoot 'work/difficulty-userdata'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    foreach($planet in $Planets) {
        $scene=if($planet -eq 'hutta'){'res://Gameplay/GameWorld_Hutta.tscn'}else{'res://Gameplay/GameWorld.tscn'}
        $receipt=Join-Path $PSScriptRoot "manifests/difficulty-$planet-smoke.json"
        if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
        $options=@('--path',$project,$scene,'--max-fps','60','--quit-after','1800','--log-file',(Join-Path $PSScriptRoot "logs/difficulty-$planet-runtime.log"))
        if(!$Rendered){$options=@('--headless')+$options}
        & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') @options -- --difficulty-smoke --advanced-class=juggernaut "--map=$planet" *> (Join-Path $PSScriptRoot "logs/difficulty-$planet-console.log")
        if($LASTEXITCODE -ne 0){throw "$planet difficulty runtime failed; see logs/difficulty-$planet-console.log"}
        if(!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed){throw "$planet difficulty checks incomplete"}
        if((Get-Content (Join-Path $PSScriptRoot "logs/difficulty-$planet-console.log") -Raw) -match 'ERROR:|SCRIPT ERROR:|Parse Error:'){throw "$planet runtime errors"}
        Write-Host "$planet difficulty checks passed."
    }
} finally {$env:APPDATA=$previousAppData}
