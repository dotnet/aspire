@ECHO OFF
SETLOCAL EnableDelayedExpansion

ECHO.
ECHO ============================================================
ECHO Restoring AspireWithMaui Playground
ECHO ============================================================
ECHO.

REM Run the main Aspire restore with MAUI workload installation
ECHO Running main Aspire restore with MAUI workload installation...
CALL "%~dp0..\..\restore.cmd" -installMaui
IF ERRORLEVEL 1 (
    ECHO ERROR: Failed to restore Aspire. Please check the output above.
    EXIT /B 1
)

ECHO.
ECHO ============================================================
ECHO Restore complete!
ECHO ============================================================
ECHO.
ECHO You can now build and run the AspireWithMaui playground.
ECHO.

EXIT /B 0
