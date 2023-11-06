@ECHO OFF
SETLOCAL

:: This command launches a Visual Studio Code with environment variables required to use a local version of the .NET Core SDK.

FOR /f "delims=" %%a IN ('where.exe code') DO @SET vscode=%%a& GOTO break
:break

IF ["%vscode%"] == [""] (
    echo [41m[ERROR][0m Visual Studio Code is not installed or can't be found.
    echo.
    exit /b 1
)

:: This tells .NET Core to use the same dotnet.exe that build scripts use
SET DOTNET_ROOT=%~dp0.dotnet
SET DOTNET_ROOT(x86)=%~dp0.dotnet\x86

:: This tells .NET Core not to go looking for .NET Core in other places
SET DOTNET_MULTILEVEL_LOOKUP=0

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
SET PATH=%DOTNET_ROOT%;%PATH%

IF NOT EXIST "%DOTNET_ROOT%\dotnet.exe" (
    echo [41m[ERROR][0m .NET SDK has not yet been installed. Run [93m%~dp0restore.cmd[0m to install.
    echo.
    exit /b 1
)

IF ["%~1"] == [""] GOTO noargs
"%vscode%" %*
exit /b 1

:noargs
"%vscode%" "."
