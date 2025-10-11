@ECHO OFF
SETLOCAL EnableDelayedExpansion

ECHO.
ECHO ============================================================
ECHO Restoring AspireWithMaui Playground
ECHO ============================================================
ECHO.

REM First, run the main Aspire restore to set up the local .dotnet SDK
ECHO [1/2] Running main Aspire restore to set up local SDK...
CALL "%~dp0..\..\restore.cmd"
IF ERRORLEVEL 1 (
    ECHO ERROR: Failed to restore Aspire. Please check the output above.
    EXIT /B 1
)

ECHO.
ECHO [2/2] Installing MAUI workload into local .dotnet...

REM Get the absolute path to the repo root (2 levels up from this script's directory)
PUSHD "%~dp0..\.."
SET "REPO_ROOT=%CD%"
POPD

REM Use the local dotnet from the repo root
SET "DOTNET_ROOT=%REPO_ROOT%\.dotnet"
SET "PATH=%DOTNET_ROOT%;%PATH%"

REM Install the MAUI workload using the local dotnet
"%DOTNET_ROOT%\dotnet.exe" workload install maui
IF ERRORLEVEL 1 (
    ECHO.
    ECHO WARNING: Failed to install MAUI workload.
    ECHO You may need to run this command manually:
    ECHO   "%DOTNET_ROOT%\dotnet.exe" workload install maui
    ECHO.
    ECHO The playground may not work without the MAUI workload installed.
    EXIT /B 1
)

ECHO.
ECHO ============================================================
ECHO Restore complete! MAUI workload is installed.
ECHO ============================================================
ECHO.
ECHO You can now build and run the AspireWithMaui playground.
ECHO.

EXIT /B 0
