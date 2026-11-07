@echo off
cd /d "%~dp0"
set "DOTNET_CLI_HOME=%~dp0.dotnet-local"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
"%ProgramFiles%\dotnet\dotnet.exe" run --project "%~dp0Hazirla.csproj"
if errorlevel 1 pause
