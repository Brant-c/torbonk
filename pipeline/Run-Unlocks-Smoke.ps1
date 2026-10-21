param()
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'godot'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Cosmetic unlocks C# build failed'}
# Isolated user data: the real Collections save is never read or written.
$previousAppData=$env:APPDATA
$smokeAppData=Join-Path $PSScriptRoot ('work/unlock-userdata-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $smokeAppData -Force | Out-Null
try {
    $env:APPDATA=$smokeAppData
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --editor --path $project --import --log-file (Join-Path $PSScriptRoot 'logs/unlock-import.log') *> (Join-Path $PSScriptRoot 'logs/unlock-import-console.log')
    if($LASTEXITCODE -ne 0){throw 'Cosmetic unlocks resource import failed'}
    $receipt=Join-Path $PSScriptRoot 'manifests/unlock-smoke.json'
    if(Test-Path -LiteralPath $receipt){Remove-Item -LiteralPath $receipt}
    $args=@('--path',$project,'--log-file',(Join-Path $PSScriptRoot 'logs/unlock-smoke.log'),'--max-fps','60','--quit-after','900','--','--unlock-smoke')
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') @args *> (Join-Path $PSScriptRoot 'logs/unlock-smoke-console.log')
    if($LASTEXITCODE -ne 0){throw 'Cosmetic unlocks runtime failed'}
    if(!(Test-Path -LiteralPath $receipt) -or !(Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).passed){throw 'Cosmetic unlocks smoke incomplete'}
} finally {$env:APPDATA=$previousAppData}
Write-Host 'Cosmetic unlock rules, legacy save, persistence and run tracker passed. Stop for manual review.'
