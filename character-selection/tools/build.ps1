$ErrorActionPreference = 'Stop'
$c1Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location -LiteralPath $c1Root
# Process-local settings: no machine/user configuration changes or shared caches.
$env:DOTNET_CLI_HOME = Join-Path $c1Root '.local/dotnet'
$env:NUGET_PACKAGES = Join-Path $c1Root '.local/nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $c1Root '.local/http-cache'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $c1Root '.local/plugin-cache'
$env:TEMP = Join-Path $c1Root 'work'
$env:TMP = $env:TEMP
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:DOTNET_NOLOGO = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME,$env:NUGET_PACKAGES,$env:TEMP,(Join-Path $c1Root 'logs') | Out-Null
& dotnet build 'SwtorSurvivors.C1.csproj' --configfile 'NuGet.Config' --disable-build-servers '-p:RestoreIgnoreFailedSources=true' *> (Join-Path $c1Root 'logs/build.log')
$c1Exit = $LASTEXITCODE
Get-Content -LiteralPath (Join-Path $c1Root 'logs/build.log') -Tail 45
exit $c1Exit
