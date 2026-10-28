$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'godot'
$buildLog = Join-Path $PSScriptRoot 'logs/npc-navigation-build.log'
$runtimeLog = Join-Path $PSScriptRoot 'logs/npc-navigation-runtime.log'
$consoleLog = Join-Path $PSScriptRoot 'logs/npc-navigation-console.log'
& dotnet build (Join-Path $project 'SwtorAssetInspection.csproj') --no-restore --nologo -p:UseSharedCompilation=false *> $buildLog
if ($LASTEXITCODE -ne 0) { throw "NPC navigation build failed: $buildLog" }
$previousAppData = $env:APPDATA
try {
    $env:APPDATA = Join-Path $PSScriptRoot 'work/npc-navigation-userdata'
    New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
    & (Join-Path $PSScriptRoot 'tools/godot/Godot_console.exe') --headless --path $project res://Gameplay/NpcNavigationSmoke.tscn --max-fps 120 --quit-after 2400 --log-file $runtimeLog *> $consoleLog
    if ($LASTEXITCODE -ne 0) { throw "NPC navigation checks failed: $consoleLog" }
} finally { $env:APPDATA = $previousAppData }
$output = Get-Content -LiteralPath $consoleLog -Raw
if ($output -notmatch 'NPC_NAVIGATION_PASSED' -or $output -match 'ERROR:|SCRIPT ERROR:|Parse Error:') {
    throw "NPC navigation checks incomplete or reported errors: $consoleLog"
}
Write-Host $output
