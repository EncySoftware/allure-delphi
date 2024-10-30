@ECHO OFF
cd %~dp0

SET DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
SET DOTNET_CLI_TELEMETRY_OPTOUT=1
SET DOTNET_MULTILEVEL_LOOKUP=0

REM CALL dotnet nuget locals http-cache --clear
CALL dotnet run --project "%~dp0build\stbuild.csproj" -- %*

EXIT /B %EXIT_CODE%