@ECHO OFF
SETLOCAL

:: This command launches a Visual Studio Code with environment variables required to use a local version of the .NET Core SDK.
:: Set VSCODE_CMD environment variable to use a different VS Code variant (e.g., code-insiders).

IF ["%VSCODE_CMD%"] == [""] SET VSCODE_CMD=code

FOR /f "delims=" %%a IN ('where.exe %VSCODE_CMD%') DO @SET vscode=%%a& GOTO break
:break

IF ["%vscode%"] == [""] (
    echo [ERROR] %VSCODE_CMD% is not installed or can't be found.
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
    echo [ERROR] .NET SDK has not yet been installed. Run %~dp0restore.cmd to install.
    echo.
    exit /b 1
)

IF ["%~1"] == [""] GOTO noargs
"%vscode%" %*
exit /b 1

:noargs
"%vscode%" "."