param([switch]$SkipZip)
$ErrorActionPreference = 'Stop'
$pipeline = Split-Path $PSScriptRoot -Parent
$source = Join-Path $pipeline 'godot'
$engine = Join-Path $pipeline 'tools/godot/Godot_console.exe'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$work = Join-Path $pipeline "work/releases/$stamp"
$stage = Join-Path $work 'project'
$release = Join-Path $pipeline "generated/releases/TorBonk-$stamp-Windows-x64"
$game = Join-Path $release 'TorBonk'
$templateArchive = Join-Path $pipeline 'tools/downloads/Godot_v4.7.2-stable_mono_export_templates.tpz'
$expectedHash = '92F8681E349EF1F90891B792DA95E3B2B0BD1ED610B78018C58FEB2D87E15A9D'
$templates = Join-Path $pipeline 'tools/godot/editor_data/export_templates/4.7.2.stable.mono'
if (!(Test-Path -LiteralPath $engine)) { throw "Missing engine: $engine" }
if (!(Test-Path -LiteralPath (Join-Path $templates 'windows_release_x86_64.exe'))) {
    if (!(Test-Path -LiteralPath $templateArchive)) { throw 'Download the official 4.7.2 .NET export TPZ into pipeline/tools/downloads first. See packaging/README.md.' }
    if ((Get-FileHash -LiteralPath $templateArchive -Algorithm SHA256).Hash -ne $expectedHash) { throw 'Export template checksum mismatch.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($templateArchive)
    try {
        New-Item -ItemType Directory -Path $templates -Force | Out-Null
        $entry = $archive.Entries | Where-Object { $_.Name -eq 'windows_release_x86_64.exe' }
        if (@($entry).Count -ne 1) { throw 'Windows .NET release template not found uniquely in archive.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $templates $entry.Name), $true)
    } finally { $archive.Dispose() }
}
New-Item -ItemType Directory -Path $stage,$game -Force | Out-Null
$env:DOTNET_CLI_HOME = Join-Path $work 'dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:APPDATA = Join-Path $work 'appdata'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
if (!$env:NUGET_PACKAGES) { $env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget/packages' }
Write-Host 'Copying current game into an isolated packaging workspace...'
& robocopy $source $stage /E /XJ /XD .godot export_templates feature_profiles script_templates text_editor_themes /XF export_presets.cfg /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw "Project copy failed: $LASTEXITCODE" }
# Reuse imported assets, while keeping editor and C# build state isolated.
& robocopy (Join-Path $source '.godot/imported') (Join-Path $stage '.godot/imported') /E /XJ /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw "Import cache copy failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'windows-export.cfg') -Destination (Join-Path $stage 'export_presets.cfg')
# Only the copied C# project changes. Keep the application and save-folder identity.
$csproj = Join-Path $stage 'SwtorAssetInspection.csproj'
$csText = [IO.File]::ReadAllText($csproj).Replace('</PropertyGroup>', '<SelfContained>true</SelfContained><NuGetAudit>false</NuGetAudit></PropertyGroup>')
[IO.File]::WriteAllText($csproj, $csText)
# A copied-project-only entry point lets the release binary exercise maps without
# Godot's editor-only --scene override. Normal startup is unchanged.
$menuPath=Join-Path $stage 'Integration/MainMenu.cs'
$menuText=[IO.File]::ReadAllText($menuPath)
$entry=@'
        if (OS.GetCmdlineUserArgs().Contains("--package-check"))
        {
            var map = MapRegistry.Find(OS.GetCmdlineUserArgs().First(a => a.StartsWith("--map="))[6..]);
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, map!.WorldScene);
            return;
        }
'@
[IO.File]::WriteAllText($menuPath,$menuText.Replace('        GetTree().Paused = false;', $entry+"`n        GetTree().Paused = false;"))
Push-Location $stage
try {
    @'
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SwtorAssetInspection", "SwtorAssetInspection.csproj", "{78A3261D-5FCF-432B-886F-2D066B6F2A89}"
EndProject
Global
EndGlobal
'@ | Set-Content -LiteralPath 'SwtorAssetInspection.sln' -Encoding UTF8
    & dotnet build SwtorAssetInspection.csproj --nologo *> (Join-Path $work 'build.log')
    if ($LASTEXITCODE -ne 0) { throw "Build failed; see $work/build.log" }
} finally { Pop-Location }
Write-Host 'Importing and exporting the standalone Windows release...'
& $engine --headless --path $stage --editor --import --log-file (Join-Path $work 'import.log') *> (Join-Path $work 'import-console.log')
if ($LASTEXITCODE -ne 0) { throw "Import failed; see $work/import-console.log" }
& $engine --headless --path $stage --export-release 'Windows Desktop' (Join-Path $game 'TorBonk.exe') --log-file (Join-Path $work 'export.log') *> (Join-Path $work 'export-console.log')
if ($LASTEXITCODE -ne 0) { throw "Export failed; see $work/export-console.log" }
$runtime = Get-ChildItem -LiteralPath $game -Recurse -Filter coreclr.dll
if (!$runtime) { throw 'Export did not include the .NET runtime.' }
if (!(Test-Path -LiteralPath (Join-Path $game 'TorBonk.pck'))) { throw 'Missing game asset package.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Godot-LICENSE.txt'),(Join-Path $PSScriptRoot 'Godot-COPYRIGHT.txt') -Destination $game
@'
TorBonk - Windows x64

Extract the complete ZIP first, then open TorBonk.exe.
Keep every included file and folder together. No editor or developer tools are needed.
WASD: move. Space: jump. Hold right mouse: rotate camera. Wheel: zoom.
Settings and cosmetic progress are stored separately in your Windows application data.
To update, extract the new version into a new folder; keep your saved application data.
This build contains SWTOR assets; see the source repository's asset permissions notice.
'@ | Set-Content -LiteralPath (Join-Path $game 'Readme.txt') -Encoding UTF8
& (Join-Path $PSScriptRoot 'Test-Windows.ps1') -GameFolder $game -StagedProject $stage
Copy-Item -LiteralPath (Join-Path $work 'release-checks/result.json') -Destination (Join-Path $release 'validation.json')
$files = Get-ChildItem -LiteralPath $game -Recurse -File | ForEach-Object {
    [ordered]@{ path=$_.FullName.Substring($game.Length+1); bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
[ordered]@{ created=(Get-Date -Format o); engine='4.7.2.stable.mono'; templateSha256=$expectedHash; files=@($files) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $release 'manifest.json') -Encoding UTF8
if (!$SkipZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($game, "$release.zip", [IO.Compression.CompressionLevel]::Optimal, $true)
    Get-FileHash -LiteralPath "$release.zip" -Algorithm SHA256 | Format-List
}
Write-Host "Release folder: $game"
Write-Host "Logs and isolated project: $work"
