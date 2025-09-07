@ECHO OFF
SETLOCAL

:: This command launches a Visual Studio solution with environment variables required to use a local version of the .NET Core SDK.

:: Download and install the .NET Core SDK if it is not already installed.
powershell -ExecutionPolicy ByPass -NoProfile -Command "& { . '%~dp0eng\common\tools.ps1'; InitializeDotNetCli $true $true }"

if NOT [%ERRORLEVEL%] == [0] (
  echo Failed to install or invoke dotnet... 1>&2
  exit /b %ERRORLEVEL%
)

:: Set the path to the .NET Core SDK used by the build scripts. This would work if using the machine wide sdk as well.
set /p dotnetPath=<%~dp0artifacts\toolset\sdk.txt

:: This tells .NET Core to use the same dotnet.exe that build scripts use
SET "DOTNET_ROOT=%dotnetPath%"
SET "DOTNET_ROOT(x86)=%dotnetPath%\x86"


:: This tells .NET Core not to go looking for .NET Core in other places
SET DOTNET_MULTILEVEL_LOOKUP=0

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
SET PATH=%DOTNET_ROOT%;%PATH%

SET sln=%~1

IF "%sln%"=="" (
    echo Solution not specified, using Aspire.slnx
    SET sln=%~dp0Aspire.slnx
)

start "" "%sln%"
