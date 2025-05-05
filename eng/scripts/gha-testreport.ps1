<#
.SYNOPSIS
    Generates a summary report from .trx test result files.

.DESCRIPTION
    This script scans a source folder for .trx test result files, parses them, and generates a summary log file in the specified destination folder.
    The summary includes test counts, outcomes, and a formatted table of results.

.PARAMETER TestResultsFolder
    The folder path where .trx test result files are located. The script will search recursively.

.PARAMETER TestSummaryOutputFolder
    The folder path where the summary log file (summary.log) will be saved. The folder will be created if it does not exist.

.EXAMPLE
    .\gha-testreport.ps1 -TestResultsFolder "C:\tests\results" -TestSummaryOutputFolder "C:\tests\summary"
#>
param(
    [Parameter(Mandatory = $true, HelpMessage = "Path to the folder containing .trx test result files.")]
    [string]$TestResultsFolder,

    [Parameter(Mandatory = $true, HelpMessage = "Path to the folder where the summary log will be saved.")]
    [string]$TestSummaryOutputFolder
)

# Ensure destination folder exists
if (-not (Test-Path $TestSummaryOutputFolder)) {
    New-Item -ItemType Directory -Path $TestSummaryOutputFolder | Out-Null
}

$trxFiles = Get-ChildItem -Path $TestResultsFolder -Filter *.trx -Recurse

$testResults = @() # Initialize an array to store test results

foreach ($trxFile in $trxFiles) {
    # Load the .trx file as XML
    $xmlContent = [xml](Get-Content -Path $trxFile.FullName)

    # Extract test results from the XML
    foreach ($testResult in $xmlContent.TestRun.Results.UnitTestResult) {
        $testName = $testResult.testName
        $outcome = $testResult.outcome
        $duration = $testResult.duration

        # Map outcome to emoji
        switch ($outcome) {
            "Passed" { $emoji = "✔️" }
            "Failed" { $emoji = "❌" }
            default { $emoji = "❔" }
        }

        # Normalize the duration to a consistent format (mm:ss.fff)
        $normalizedDuration = [TimeSpan]::Parse($duration).ToString("mm\:ss\.fff")

        # Add the test result to the array
        $testResults += [PSCustomObject]@{
            TestName    = $testName
            Outcome     = $outcome
            OutcomeIcon = $emoji
            Duration    = $normalizedDuration
        }
    }
}

if ($testResults.Length -lt 1) {
    echo "::notice:: Tests Summary: no tests found"
    return;
}

# Sort the test results by test name
$testResults = $testResults | Sort-Object -Property TestName

# Calculate summary statistics
$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Outcome -eq "Passed" }).Count
$failedTests = ($testResults | Where-Object { $_.Outcome -eq "Failed" }).Count
$skippedTests = ($testResults | Where-Object { $_.Outcome -eq "NotExecuted" }).Count

# Add the summary to the annotation
$summary = "total: $totalTests, passed: $passedTests, failed: $failedTests, skipped: $skippedTests"
if ($failedTests -gt 0) {
    echo "::warning:: Tests Summary: $summary"
} else {
    echo "::notice:: Tests Summary: $summary"
}

# Format the test results as a console-friendly table
$tableHeader = "{0,-16} {1,-150} {2,-20}" -f "Duration", "Test Name", "Result"
$tableSeparator = "-" * 185
$tableRows = $testResults | ForEach-Object { "{0,-16} {1,-150} {2,-20}" -f $_.Duration, $_.TestName, "$($_.OutcomeIcon) $($_.Outcome)" }
$table = "$tableHeader`n$tableSeparator`n" + ($tableRows -join "`n") + "`n$tableSeparator`n"
echo "`nTest Results:`n$table"

# Save the results to a file for further processing
$outputPath = Join-Path $TestSummaryOutputFolder "summary.log"
$table | Out-File -FilePath $outputPath -Encoding utf8

echo "Test results saved to $outputPath"
