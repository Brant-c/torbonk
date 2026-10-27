param([Parameter(Mandatory=$true)][string]$GameFolder,
      [Parameter(Mandatory=$true)][string]$StagedProject)
$ErrorActionPreference='Stop'
$game=(Resolve-Path -LiteralPath $GameFolder).Path
$stage=(Resolve-Path -LiteralPath $StagedProject).Path
$checks=Join-Path (Split-Path $stage -Parent) 'release-checks'
New-Item -ItemType Directory -Path $checks -Force | Out-Null
$env:APPDATA=Join-Path $checks 'appdata'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
$extensions=@('.tscn','.tres','.glb','.png','.jpg','.jpeg','.wav','.ogg','.ttf','.otf','.gdshader','.json')
$paths=@(Get-ChildItem -LiteralPath (Join-Path $stage 'Assets'),(Join-Path $stage 'Frontend'),(Join-Path $stage 'Gameplay'),(Join-Path $stage 'Integration') -Recurse -File |
    Where-Object {$_.Extension -in $extensions} | ForEach-Object {'res://'+$_.FullName.Substring($stage.Length+1).Replace('\','/')})
$paths | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $checks 'resources.json') -Encoding UTF8
$exe=Join-Path $game 'TorBonk.exe'
$validator=Join-Path $PSScriptRoot 'Verify-Pack.gd'
$engine=Join-Path (Split-Path $PSScriptRoot -Parent) 'tools/godot/Godot_console.exe'
Push-Location $game
try {
    $resourceLog=Join-Path $checks 'resources.log'
    # --script is an editor command; inspect the exported pack using the editor,
    # then use the actual release executable for all startup tests below.
    & $engine --headless --path $game --main-pack (Join-Path $game 'TorBonk.pck') --script $validator --log-file $resourceLog -- --check-list (Join-Path $checks 'resources.json') *> (Join-Path $checks 'resources-console.log')
    if ($LASTEXITCODE -ne 0) {throw "Pack resource check failed; see $resourceLog"}
    $inventoryLine=Get-Content -LiteralPath $resourceLog | Where-Object {$_ -match '^\{"checked"'} | Select-Object -Last 1
    if(!$inventoryLine) {throw 'Pack inspector did not report an inventory result.'}
    $inventory=$inventoryLine | ConvertFrom-Json
    if($inventory.checked -ne $paths.Count -or $inventory.missing.Count -or $inventory.invalid_json.Count) {throw 'Pack inventory was incomplete.'}
    $scenes=@('MainMenu','korriban','hutta','dromund','balmorra')
    $profiles=Get-Content -LiteralPath (Join-Path $stage 'Gameplay/classes.json') -Raw | ConvertFrom-Json
    $scenes+=@($profiles | Where-Object {$_.Id -ne 'sorcerer'} | ForEach-Object {'korriban__'+$_.Id})
    foreach($name in $scenes){
        $runtimeLog=Join-Path $checks "$name.log"
        $arguments=@('--headless','--quit-after','180','--log-file',('"'+$runtimeLog+'"'))
        if($name -ne 'MainMenu') {
            $parts=$name -split '__'
            $class=if($parts.Count -gt 1){$parts[1]}else{'sorcerer'}
            $arguments+=@('--','--package-check',("--map="+$parts[0]),"--advanced-class=$class")
        }
        $process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -RedirectStandardOutput (Join-Path $checks "$name-stdout.txt") -RedirectStandardError (Join-Path $checks "$name-stderr.txt") -PassThru
        if(!$process.WaitForExit(60000)) {$process.Kill();throw "Runtime check timed out for $name"}
        if($process.ExitCode -ne 0 -or (Select-String -LiteralPath $runtimeLog -Pattern 'SCRIPT ERROR:|ERROR:|Unhandled exception' -Quiet)){throw "Runtime check failed for $name; see $runtimeLog"}
    }
} finally {Pop-Location}
[ordered]@{passed=$true; resources=$paths.Count; scenes=$scenes; note='Headless package checks on build machine; separate hardware and interactive playtesting still required.'} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $checks 'result.json') -Encoding UTF8
Write-Host "Passed. Results: $checks/result.json"
