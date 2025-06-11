@echo off
REM Git bisect helper script for investigating WithHttpCommand_ResultsInExpectedResultForHttpMethod test failures
REM This script automates the git bisect process to find the commit that introduced repeated test failures.
REM
REM Usage: withhttpcommand-bisect.cmd <good-commit> [bad-commit]
REM   good-commit: A known good commit hash
REM   bad-commit:  A known bad commit hash (defaults to HEAD)
REM
REM Example: withhttpcommand-bisect.cmd abc123def main
REM

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\..\"
set "TEST_PROJECT=tests\Aspire.Hosting.Tests\Aspire.Hosting.Tests.csproj"
set "TEST_FILTER=WithHttpCommand_ResultsInExpectedResultForHttpMethod"
set "ITERATIONS=10"

REM Function to display usage
if "%1"=="bisect_test" goto bisect_test
if "%1"=="" goto usage
if "%1"=="/?" goto usage
if "%1"=="-h" goto usage
if "%1"=="--help" goto usage

REM Parse arguments
set "GOOD_COMMIT=%1"
set "BAD_COMMIT=%2"
if "%BAD_COMMIT%"=="" set "BAD_COMMIT=HEAD"

REM Function to log messages with timestamps
goto main

:log
echo [%date% %time%] %*
exit /b

:usage
echo Usage: %0 ^<good-commit^> [bad-commit]
echo   good-commit: A known good commit hash
echo   bad-commit:  A known bad commit hash (defaults to HEAD)
echo.
echo Example:
echo   %0 abc123def
echo   %0 abc123def main
exit /b 1

:run_test_iterations
call :log "Running test %ITERATIONS% times..."

for /l %%i in (1,1,%ITERATIONS%) do (
    call :log "Iteration %%i/%ITERATIONS%"
    
    REM Run the specific test (note: Windows timeout command syntax is different)
    "%REPO_ROOT%dotnet.cmd" test "%REPO_ROOT%%TEST_PROJECT%" --no-build --logger "console;verbosity=quiet" -- --filter "%TEST_FILTER%" >nul 2>&1
    if !errorlevel! neq 0 (
        call :log "Test failed on iteration %%i"
        exit /b 1
    )
    
    REM Small delay between iterations to avoid potential timing issues
    timeout /t 1 /nobreak >nul 2>&1
)

call :log "All %ITERATIONS% iterations passed"
exit /b 0

:build_project
call :log "Building project..."
REM Note: Windows timeout doesn't work the same way as Unix timeout
REM Just run the build directly - timeout handling should be done externally if needed
"%REPO_ROOT%build.cmd" --configuration Debug >nul 2>&1
if !errorlevel! neq 0 (
    call :log "Build failed"
    exit /b 1
)
call :log "Build successful"
exit /b 0

:bisect_test
for /f "tokens=*" %%a in ('git rev-parse --short HEAD') do set "SHORT_COMMIT=%%a"
call :log "Testing commit: %SHORT_COMMIT%"

REM Try to build first
call :build_project
if !errorlevel! neq 0 (
    call :log "Build failed - skipping this commit"
    exit 125
)

REM Run the test iterations
call :run_test_iterations
if !errorlevel! equ 0 (
    call :log "This commit is GOOD"
    exit 0
) else (
    call :log "This commit is BAD"
    exit 1
)

:main
call :log "Starting git bisect for WithHttpCommand_ResultsInExpectedResultForHttpMethod test"
call :log "Good commit: %GOOD_COMMIT%"
call :log "Bad commit: %BAD_COMMIT%"
call :log "Test iterations per commit: %ITERATIONS%"

cd /d "%REPO_ROOT%"

REM Ensure we're in a clean state
git diff --quiet >nul 2>&1
if !errorlevel! neq 0 (
    call :log "Error: Repository has uncommitted changes. Please commit or stash them first."
    exit /b 1
)

git diff --cached --quiet >nul 2>&1
if !errorlevel! neq 0 (
    call :log "Error: Repository has staged changes. Please commit or unstage them first."
    exit /b 1
)

REM Validate commits exist
git rev-parse --verify "%GOOD_COMMIT%" >nul 2>&1
if !errorlevel! neq 0 (
    call :log "Error: Good commit '%GOOD_COMMIT%' does not exist"
    exit /b 1
)

git rev-parse --verify "%BAD_COMMIT%" >nul 2>&1
if !errorlevel! neq 0 (
    call :log "Error: Bad commit '%BAD_COMMIT%' does not exist"
    exit /b 1
)

REM Store original branch/commit for cleanup
for /f "tokens=*" %%a in ('git symbolic-ref --short HEAD 2^>nul ^|^| git rev-parse HEAD') do set "ORIGINAL_REF=%%a"

REM Start bisect
call :log "Starting git bisect..."
git bisect start
git bisect bad "%BAD_COMMIT%"
git bisect good "%GOOD_COMMIT%"

REM Create a temporary script for bisect run
set "TEMP_SCRIPT=%TEMP%\bisect-test-%RANDOM%.cmd"
echo @echo off > "%TEMP_SCRIPT%"
echo setlocal enabledelayedexpansion >> "%TEMP_SCRIPT%"
echo set "REPO_ROOT=%REPO_ROOT%" >> "%TEMP_SCRIPT%"
echo set "TEST_PROJECT=%TEST_PROJECT%" >> "%TEMP_SCRIPT%"
echo set "TEST_FILTER=%TEST_FILTER%" >> "%TEMP_SCRIPT%"
echo set "ITERATIONS=%ITERATIONS%" >> "%TEMP_SCRIPT%"
echo. >> "%TEMP_SCRIPT%"
echo call "%~f0" bisect_test >> "%TEMP_SCRIPT%"

REM Run the bisect
call :log "Running automated bisect..."
git bisect run "%TEMP_SCRIPT%"
set "BISECT_EXIT_CODE=!errorlevel!"

REM Cleanup temp script
if exist "%TEMP_SCRIPT%" del "%TEMP_SCRIPT%"

REM Show the result
if !BISECT_EXIT_CODE! equ 0 (
    call :log "Bisect completed!"
    call :log "The problematic commit is:"
    for /f "tokens=*" %%a in ('git show --no-patch --format^="%%H %%s" HEAD') do call :log "%%a"
    
    REM Save bisect log
    for /f "tokens=2 delims= " %%a in ('date /t') do set "CURRENT_DATE=%%a"
    for /f "tokens=1 delims= " %%a in ('time /t') do set "CURRENT_TIME=%%a"
    set "CURRENT_DATE=!CURRENT_DATE:/=!"
    set "CURRENT_TIME=!CURRENT_TIME::=!"
    set "CURRENT_TIME=!CURRENT_TIME:.=!"
    set "BISECT_LOG=%REPO_ROOT%bisect-withhttpcommand-!CURRENT_DATE!-!CURRENT_TIME!.log"
    git bisect log > "!BISECT_LOG!"
    call :log "Bisect log saved to: !BISECT_LOG!"
) else (
    call :log "Bisect may not have completed successfully. Check the git state."
)

REM Cleanup
call :log "Cleaning up..."
git bisect reset >nul 2>&1
git rev-parse --verify "%ORIGINAL_REF%" >nul 2>&1
if !errorlevel! equ 0 (
    git checkout "%ORIGINAL_REF%" >nul 2>&1
)
call :log "Repository state restored"

exit /b 0