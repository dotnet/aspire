<#
.SYNOPSIS
    Repeatedly runs a dotnet test command to validate flaky test fixes.

.DESCRIPTION
    Runs <TestCommand> repeatedly to validate flaky test fixes.
    Cleanup logic modeled on tests/helix/send-to-helix-inner.proj pre-commands.
    Everything after '--' is executed verbatim each iteration.

.PARAMETER n
    Number of iterations (default: 100).

.PARAMETER RunAll
    Don't stop on first failure, run all iterations.

.EXAMPLE
    ./run-test-repeatedly.ps1 -- dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj --no-build -- --filter-method "*.MyTest"

.EXAMPLE
    ./run-test-repeatedly.ps1 -n 50 -RunAll -- dotnet test tests/Foo/Foo.csproj -- --filter-method "*.Bar"
#>

# ---------- defaults ----------
$Iterations = 100
$StopOnFailure = $true

# ---------- parse arguments ----------
$TestCmd = @()
$i = 0
while ($i -lt $args.Count) {
    switch ($args[$i]) {
        '-n' {
            $Iterations = [int]$args[$i + 1]
            $i += 2
        }
        '--run-all' {
            $StopOnFailure = $false
            $i++
        }
        '--help' {
            Write-Host @"
Usage: ./run-test-repeatedly.ps1 [OPTIONS] -- <test command...>

Runs <test command> repeatedly to validate flaky test fixes.
Everything after '--' is executed verbatim each iteration.

Options:
  -n <count>    Number of iterations (default: 100)
  --run-all     Don't stop on first failure, run all iterations
  --help        Show this help message

Examples:
  ./run-test-repeatedly.ps1 -- dotnet test tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj --no-build -- --filter-method "*.MyTest"
  ./run-test-repeatedly.ps1 -n 50 --run-all -- dotnet test tests/Foo/Foo.csproj -- --filter-method "*.Bar"
"@
            exit 0
        }
        '--' {
            $TestCmd = $args[($i + 1)..($args.Count - 1)]
            break
        }
        default {
            Write-Error "Unknown option: $($args[$i])"
            Write-Error "Run with --help for usage."
            exit 1
        }
    }
}

if ($TestCmd.Count -eq 0) {
    Write-Error "Error: no test command provided. Use -- to separate options from the test command."
    Write-Error "Run with --help for usage."
    exit 1
}

# ---------- infer test assembly name from .csproj in args ----------
$TestAssemblyName = ""
$TestProjectDir = ""
foreach ($arg in $TestCmd) {
    if ($arg -like '*.csproj') {
        $TestAssemblyName = [System.IO.Path]::GetFileNameWithoutExtension($arg)
        $TestProjectDir = Split-Path -Parent $arg
        break
    }
}

# ---------- setup ----------
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ResultsDir = Join-Path $env:TEMP "test-results-$timestamp"
New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
$LogFile = Join-Path $ResultsDir "test-run.log"

$PassCount = 0
$FailCount = 0
$TimeoutCount = 0

# ---------- cleanup ----------
function Clean-Environment {
    # Kill dcp / dcpctrl processes
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -match '^dcp(ctl)?$'
    } | ForEach-Object {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }

    # Kill dotnet-tests processes (actual test runner)
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -eq 'dotnet-tests'
    } | ForEach-Object {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }

    # Kill test service processes (TestProject.Service*, TestProject.Worker*)
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -match '^TestProject\.(Service|Worker)'
    } | ForEach-Object {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }

    # Brief wait for processes to die
    Start-Sleep -Seconds 1

    # Docker cleanup — only if docker is available
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $containers = docker ps -aq 2>$null
        if ($containers) {
            $containers | ForEach-Object { docker stop $_ 2>$null } | Out-Null
            $containers | ForEach-Object { docker rm $_ 2>$null } | Out-Null
        }

        $volumes = docker volume ls -q 2>$null
        if ($volumes) {
            $volumes | ForEach-Object { docker volume rm $_ 2>$null } | Out-Null
        }

        docker network prune -f 2>$null | Out-Null
    }

    # Clean test result directories under the project path
    if ($TestProjectDir -and (Test-Path (Join-Path $TestProjectDir "TestResults"))) {
        Remove-Item -Recurse -Force (Join-Path $TestProjectDir "TestResults") -ErrorAction SilentlyContinue
    }
}

# ---------- state logging ----------
function Log-EnvironmentState {
    param(
        [int]$Iteration,
        [string]$LogTarget
    )

    $logContent = @()
    $logContent += "--- Environment state before cleanup (iteration $Iteration) ---"

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $logContent += ">> docker container ls --all"
        $logContent += (docker container ls --all 2>&1 | Out-String)
        $logContent += ">> docker volume ls"
        $logContent += (docker volume ls 2>&1 | Out-String)
        $logContent += ">> docker network ls"
        $logContent += (docker network ls 2>&1 | Out-String)
    }

    $logContent += ">> processes (dcp|dotnet)"
    $procs = Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -match 'dcp|dotnet'
    } | Select-Object Id, ProcessName | Out-String
    if ($procs.Trim()) {
        $logContent += $procs
    } else {
        $logContent += "(none)"
    }
    $logContent += "--- end environment state ---"

    $logContent | Out-File -Append -FilePath $LogTarget -Encoding utf8
}

# ---------- header ----------
$header = @"
========================================
Test Verification Run — $Iterations iterations
Mode: $(if ($StopOnFailure) { 'STOP ON FIRST FAILURE' } else { 'RUN ALL' })
========================================
Command: $($TestCmd -join ' ')
Test assembly: $(if ($TestAssemblyName) { $TestAssemblyName } else { '(unknown)' })
Results: $ResultsDir
Started: $(Get-Date)
Git commit: $(try { git rev-parse HEAD 2>$null } catch { 'N/A' })
========================================

"@
Write-Host $header
$header | Out-File -Append -FilePath $LogFile -Encoding utf8

# ---------- main loop ----------
$FirstFailureIteration = 0
$TimeoutSeconds = 180

for ($iter = 1; $iter -le $Iterations; $iter++) {
    $iterLog = Join-Path $ResultsDir "iteration-$iter.log"

    # Log environment state, then clean
    Log-EnvironmentState -Iteration $iter -LogTarget $iterLog
    Clean-Environment

    # Print iteration header
    $iterHeader = "Iteration $iter/$Iterations [$(Get-Date -Format 'HH:mm:ss')]: "
    Write-Host -NoNewline $iterHeader
    $iterHeader | Out-File -Append -FilePath $LogFile -Encoding utf8 -NoNewline

    # Log exact command
    "Running: $($TestCmd -join ' ')" | Out-File -Append -FilePath $iterLog -Encoding utf8

    # Run test with timeout
    $cmdString = $TestCmd -join ' '
    $process = Start-Process -FilePath $TestCmd[0] -ArgumentList $TestCmd[1..($TestCmd.Count - 1)] `
        -NoNewWindow -PassThru -RedirectStandardOutput (Join-Path $ResultsDir "iter-stdout-$iter.tmp") `
        -RedirectStandardError (Join-Path $ResultsDir "iter-stderr-$iter.tmp")

    $completed = $process.WaitForExit($TimeoutSeconds * 1000)

    if (-not $completed) {
        # Process timed out
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $exitCode = 124
    } else {
        $exitCode = $process.ExitCode
    }

    # Append stdout/stderr to iteration log
    $stdoutFile = Join-Path $ResultsDir "iter-stdout-$iter.tmp"
    $stderrFile = Join-Path $ResultsDir "iter-stderr-$iter.tmp"
    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | Out-File -Append -FilePath $iterLog -Encoding utf8
        Remove-Item $stdoutFile -ErrorAction SilentlyContinue
    }
    if (Test-Path $stderrFile) {
        Get-Content $stderrFile | Out-File -Append -FilePath $iterLog -Encoding utf8
        Remove-Item $stderrFile -ErrorAction SilentlyContinue
    }

    # Classify result
    if ($exitCode -eq 0) {
        Write-Host "PASS" -ForegroundColor Green
        "PASS" | Out-File -Append -FilePath $LogFile -Encoding utf8
        $PassCount++
    } elseif ($exitCode -eq 124) {
        Write-Host "TIMEOUT" -ForegroundColor Yellow
        "TIMEOUT" | Out-File -Append -FilePath $LogFile -Encoding utf8
        $TimeoutCount++
        Copy-Item $iterLog (Join-Path $ResultsDir "timeout-$iter.log")
    } else {
        Write-Host "FAIL (exit $exitCode)" -ForegroundColor Red
        "FAIL (exit $exitCode)" | Out-File -Append -FilePath $LogFile -Encoding utf8
        $FailCount++
        Copy-Item $iterLog (Join-Path $ResultsDir "failure-$iter.log")
    }

    # Handle failure
    if ($exitCode -ne 0) {
        if ($FirstFailureIteration -eq 0) {
            $FirstFailureIteration = $iter
        }
        if ($StopOnFailure) {
            Write-Host ""
            Write-Host "Stopping at iteration $iter due to failure." -ForegroundColor Red
            "" | Out-File -Append -FilePath $LogFile -Encoding utf8
            "Stopping at iteration $iter due to failure." | Out-File -Append -FilePath $LogFile -Encoding utf8
            break
        }
    }

    # Progress every 10 iterations
    if ($iter % 10 -eq 0) {
        $progressMsg = "  Progress: $iter/$Iterations - Pass: $PassCount, Fail: $FailCount, Timeout: $TimeoutCount"
        Write-Host $progressMsg -ForegroundColor Blue
        $progressMsg | Out-File -Append -FilePath $LogFile -Encoding utf8
    }
}

# ---------- final cleanup ----------
Clean-Environment

# ---------- summary ----------
$Total = $PassCount + $FailCount + $TimeoutCount
function Get-Pct { param([int]$num, [int]$den); if ($den -eq 0) { return "0.0" }; return "{0:F1}" -f (($num / $den) * 100) }

Write-Host ""
Write-Host "========================================"
Write-Host "Summary"
Write-Host "========================================"

$summaryLines = @("")
$summaryLines += "========================================"
$summaryLines += "Summary"
$summaryLines += "========================================"

if ($FirstFailureIteration -gt 0 -and $StopOnFailure) {
    Write-Host "Stopped at iteration $FirstFailureIteration" -ForegroundColor Red
    $summaryLines += "Stopped at iteration $FirstFailureIteration"
}

$completedMsg = "Completed: $Total / $Iterations"
Write-Host $completedMsg
$summaryLines += $completedMsg

$passMsg = "  Pass:    $PassCount ($(Get-Pct $PassCount $Total)%)"
Write-Host $passMsg -ForegroundColor Green
$summaryLines += $passMsg

$failMsg = "  Fail:    $FailCount ($(Get-Pct $FailCount $Total)%)"
Write-Host $failMsg -ForegroundColor Red
$summaryLines += $failMsg

$timeoutMsg = "  Timeout: $TimeoutCount ($(Get-Pct $TimeoutCount $Total)%)"
Write-Host $timeoutMsg -ForegroundColor Yellow
$summaryLines += $timeoutMsg

Write-Host "========================================"
Write-Host "Finished: $(Get-Date)"
Write-Host "Results:  $ResultsDir"

$summaryLines += "========================================"
$summaryLines += "Finished: $(Get-Date)"
$summaryLines += "Results:  $ResultsDir"

if ($FailCount -eq 0 -and $TimeoutCount -eq 0 -and $Total -eq $Iterations) {
    Write-Host ""
    Write-Host "All $Iterations iterations passed." -ForegroundColor Green
    $summaryLines += ""
    $summaryLines += "All $Iterations iterations passed."
    $summaryLines | Out-File -Append -FilePath $LogFile -Encoding utf8
    exit 0
} else {
    Write-Host ""
    Write-Host "Failure logs: $ResultsDir\failure-*.log"
    Write-Host "Timeout logs: $ResultsDir\timeout-*.log"
    $summaryLines += ""
    $summaryLines += "Failure logs: $ResultsDir\failure-*.log"
    $summaryLines += "Timeout logs: $ResultsDir\timeout-*.log"
    $summaryLines | Out-File -Append -FilePath $LogFile -Encoding utf8
    exit 1
}
