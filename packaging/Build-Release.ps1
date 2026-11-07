param([string]$InnoCompiler = "$PSScriptRoot\..\build-tools\InnoSetup\ISCC.exe")
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME=Join-Path $root '.dotnet-local'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$publish=Join-Path $root 'dist\Hazirla-0.1.0-win-x64'
& "$env:ProgramFiles\dotnet\dotnet.exe" publish (Join-Path $root 'Hazirla\Hazirla.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o $publish --disable-build-servers
if($LASTEXITCODE -ne 0){throw 'Yayınlama başarısız.'}
$packages=Join-Path $env:DOTNET_CLI_HOME '.nuget\packages'
Copy-Item (Join-Path $packages 'skiasharp\2.88.9\LICENSE.txt') (Join-Path $publish 'SkiaSharp-LICENSE.txt')
Copy-Item (Join-Path $packages 'skiasharp.nativeassets.win32\2.88.9\THIRD-PARTY-NOTICES.txt') (Join-Path $publish 'SkiaSharp-NOTICES.txt')
$config=Get-Content (Join-Path $publish 'Hazirla.runtimeconfig.json') -Raw | ConvertFrom-Json
$runtimeVersion=($config.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
Copy-Item (Join-Path $packages "microsoft.netcore.app.runtime.win-x64\$runtimeVersion\LICENSE.TXT") (Join-Path $publish 'DotNet-LICENSE.txt')
Copy-Item (Join-Path $packages "microsoft.netcore.app.runtime.win-x64\$runtimeVersion\THIRD-PARTY-NOTICES.TXT") (Join-Path $publish 'DotNet-NOTICES.txt')
Copy-Item (Join-Path $PSScriptRoot 'KULLANIM.txt') $publish
if(!(Test-Path -LiteralPath $InnoCompiler)){throw 'Inno Setup derleyicisi bulunamadı.'}
& $InnoCompiler /Qp (Join-Path $PSScriptRoot 'Hazirla.iss')
if($LASTEXITCODE -ne 0){throw 'Yükleyici üretilemedi.'}
$zip=Join-Path $root 'dist\Hazirla-0.1.0-win-x64-portable.zip'
Compress-Archive -Path $publish -DestinationPath $zip -Force
Get-FileHash (Join-Path $root 'dist\Hazirla-0.1.0-win-x64-Setup.exe'),$zip | Format-Table
