param()
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'D1.1 C# build failed'}
$previousAppData=$env:APPDATA
$smokeAppData=Join-Path $PSScriptRoot ('work/d11-userdata-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA=$smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/d11-import.log') *> (Join-Path $PSScriptRoot 'logs/d11-import-console.log')
    if($LASTEXITCODE -ne 0){throw 'D1.1 resource import failed'}
    # A single short wardrobe review, then one application restart to verify disk state and launch gameplay.
    foreach($phase in @('save','load')) {
        $receipt=Join-Path $PSScriptRoot ('manifests/d11-'+$phase+'-smoke.json')
        if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
        $args=@('--path',$project,'--log-file',(Join-Path $PSScriptRoot ('logs/d11-'+$phase+'.log')),'--max-fps','60','--quit-after','900','--',('--d11-smoke-'+$phase))
        if($phase -eq 'save'){$args+='--wardrobe-dev'}
        & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') @args *> (Join-Path $PSScriptRoot ('logs/d11-'+$phase+'-console.log'))
        if($LASTEXITCODE -ne 0){throw ('D1.1 '+$phase+' runtime failed')}
        if(!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed){throw ('D1.1 '+$phase+' incomplete')}
    }
} finally {$env:APPDATA=$previousAppData}
Write-Host 'D1.1 compile, wardrobe, restart persistence and gameplay handoff passed. Stop for manual review.'
