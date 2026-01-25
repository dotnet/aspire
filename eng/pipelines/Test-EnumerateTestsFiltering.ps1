<#
.SYNOPSIS
    Test harness for enumerate-tests action filtering logic

.DESCRIPTION
    Tests the project filtering logic used in .github/actions/enumerate-tests/action.yml
    This validates the shortname conversion and filtering behavior.

.EXAMPLE
    ./Test-EnumerateTestsFiltering.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Test tracking
$script:Passed = 0
$script:Failed = 0
$script:Failures = @()

function Write-TestHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "=== $Message ===" -ForegroundColor Blue
}

function Convert-ProjectPathToShortname {
    <#
    .SYNOPSIS
        Converts a project path to its shortname (same logic as enumerate-tests action)
    #>
    param([string]$ProjectPath)

    # Extract directory name from path (handle trailing slash)
    $dirName = ($ProjectPath -replace '/$', '') -split '/' | Select-Object -Last 1
    # Convert to shortname: Aspire.Milvus.Client.Tests -> Milvus.Client
    $shortname = $dirName -replace '^Aspire\.', '' -replace '\.Tests$', ''
    return $shortname
}

function Apply-ProjectsFilter {
    <#
    .SYNOPSIS
        Applies the projects filter to a list of test shortnames (same logic as enumerate-tests action)
    #>
    param(
        [string[]]$AllTests,
        [string]$ProjectsFilterJson
    )

    if (-not $ProjectsFilterJson -or $ProjectsFilterJson -eq '' -or $ProjectsFilterJson -eq '[]') {
        return $AllTests
    }

    $projects = $ProjectsFilterJson | ConvertFrom-Json
    $allowedShortnames = @()

    foreach ($project in $projects) {
        $shortname = Convert-ProjectPathToShortname -ProjectPath $project
        $allowedShortnames += $shortname
    }

    if ($allowedShortnames.Count -gt 0) {
        return @($AllTests | Where-Object { $_ -in $allowedShortnames })
    }

    return $AllTests
}

function Test-ShortnameConversion {
    param(
        [string]$Name,
        [string]$ProjectPath,
        [string]$ExpectedShortname
    )

    $actual = Convert-ProjectPathToShortname -ProjectPath $ProjectPath

    if ($actual -eq $ExpectedShortname) {
        Write-Host "PASS $Name" -ForegroundColor Green
        $script:Passed++
    }
    else {
        Write-Host "FAIL $Name" -ForegroundColor Red
        Write-Host "     Input: $ProjectPath"
        Write-Host "     Expected: $ExpectedShortname"
        Write-Host "     Actual: $actual"
        $script:Failed++
        $script:Failures += $Name
    }
}

function Test-Filtering {
    param(
        [string]$Name,
        [string[]]$AllTests,
        [string]$ProjectsFilterJson,
        [string[]]$ExpectedTests
    )

    $actual = @(Apply-ProjectsFilter -AllTests $AllTests -ProjectsFilterJson $ProjectsFilterJson)
    $expected = @($ExpectedTests | Sort-Object)
    $actualSorted = @($actual | Sort-Object)

    $match = ($actualSorted -join ',') -eq ($expected -join ',')

    if ($match) {
        Write-Host "PASS $Name" -ForegroundColor Green
        $script:Passed++
    }
    else {
        Write-Host "FAIL $Name" -ForegroundColor Red
        Write-Host "     Filter: $ProjectsFilterJson"
        Write-Host "     Expected: $($expected -join ', ')"
        Write-Host "     Actual: $($actualSorted -join ', ')"
        $script:Failed++
        $script:Failures += $Name
    }
}

# Main test execution
Write-Host "=== Test Harness for enumerate-tests Filtering Logic ==="

Write-TestHeader "Shortname Conversion Tests"

Test-ShortnameConversion -Name "SC1: Components project" `
    -ProjectPath "tests/Aspire.Milvus.Client.Tests/" `
    -ExpectedShortname "Milvus.Client"

Test-ShortnameConversion -Name "SC2: Components project without trailing slash" `
    -ProjectPath "tests/Aspire.Milvus.Client.Tests" `
    -ExpectedShortname "Milvus.Client"

Test-ShortnameConversion -Name "SC3: Hosting extension project" `
    -ProjectPath "tests/Aspire.Hosting.Redis.Tests/" `
    -ExpectedShortname "Hosting.Redis"

Test-ShortnameConversion -Name "SC4: Azure project" `
    -ProjectPath "tests/Aspire.Azure.AI.OpenAI.Tests/" `
    -ExpectedShortname "Azure.AI.OpenAI"

Test-ShortnameConversion -Name "SC5: Simple project" `
    -ProjectPath "tests/Aspire.Npgsql.Tests/" `
    -ExpectedShortname "Npgsql"

Test-ShortnameConversion -Name "SC6: Dashboard project" `
    -ProjectPath "tests/Aspire.Dashboard.Tests/" `
    -ExpectedShortname "Dashboard"

Test-ShortnameConversion -Name "SC7: Hosting project" `
    -ProjectPath "tests/Aspire.Hosting.Tests/" `
    -ExpectedShortname "Hosting"

Test-ShortnameConversion -Name "SC8: StackExchange Redis" `
    -ProjectPath "tests/Aspire.StackExchange.Redis.Tests/" `
    -ExpectedShortname "StackExchange.Redis"

Write-TestHeader "Filtering Tests"

# Sample test list (represents what GetTestProjects.proj would enumerate)
$sampleTests = @(
    "Azure.AI.OpenAI",
    "Dashboard",
    "Hosting",
    "Hosting.Azure",
    "Hosting.Redis",
    "Milvus.Client",
    "MongoDB.Driver",
    "Npgsql",
    "RabbitMQ.Client",
    "StackExchange.Redis"
)

Test-Filtering -Name "FT1: Single project filter" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Milvus.Client.Tests/"]' `
    -ExpectedTests @("Milvus.Client")

Test-Filtering -Name "FT2: Multiple projects filter" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Npgsql.Tests/","tests/Aspire.StackExchange.Redis.Tests/"]' `
    -ExpectedTests @("Npgsql", "StackExchange.Redis")

Test-Filtering -Name "FT3: Empty array filter (run all)" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '[]' `
    -ExpectedTests $sampleTests

Test-Filtering -Name "FT4: Empty string filter (run all)" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '' `
    -ExpectedTests $sampleTests

Test-Filtering -Name "FT5: Null filter (run all)" `
    -AllTests $sampleTests `
    -ProjectsFilterJson $null `
    -ExpectedTests $sampleTests

Test-Filtering -Name "FT6: Hosting extension filter" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Hosting.Redis.Tests/"]' `
    -ExpectedTests @("Hosting.Redis")

Test-Filtering -Name "FT7: Non-matching project (empty result)" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.NonExistent.Tests/"]' `
    -ExpectedTests @()

Test-Filtering -Name "FT8: Mix of matching and non-matching" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Npgsql.Tests/","tests/Aspire.NonExistent.Tests/"]' `
    -ExpectedTests @("Npgsql")

Test-Filtering -Name "FT9: Path without trailing slash" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Dashboard.Tests"]' `
    -ExpectedTests @("Dashboard")

Test-Filtering -Name "FT10: Multiple Hosting projects" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Hosting.Tests/","tests/Aspire.Hosting.Azure.Tests/","tests/Aspire.Hosting.Redis.Tests/"]' `
    -ExpectedTests @("Hosting", "Hosting.Azure", "Hosting.Redis")

Test-Filtering -Name "FT11: Azure project filter" `
    -AllTests $sampleTests `
    -ProjectsFilterJson '["tests/Aspire.Azure.AI.OpenAI.Tests/"]' `
    -ExpectedTests @("Azure.AI.OpenAI")

Write-TestHeader "Real Project Tests"

# Get actual test directories from repo to verify against real data
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$TestsDir = Join-Path $RepoRoot "tests"

if (Test-Path $TestsDir) {
    $realTestDirs = Get-ChildItem -Path $TestsDir -Directory |
        Where-Object { $_.Name -match "\.Tests$" -and $_.Name -notmatch "(EndToEnd|Templates|Cli\.EndToEnd|Shared)" } |
        Select-Object -ExpandProperty Name

    $realShortnames = $realTestDirs | ForEach-Object {
        $_ -replace '^Aspire\.', '' -replace '\.Tests$', ''
    }

    # Test with real projects
    if ($realShortnames -contains "Npgsql" -and $realShortnames -contains "Dashboard") {
        Test-Filtering -Name "RT1: Real projects - Npgsql + Dashboard" `
            -AllTests $realShortnames `
            -ProjectsFilterJson '["tests/Aspire.Npgsql.Tests/","tests/Aspire.Dashboard.Tests/"]' `
            -ExpectedTests @("Npgsql", "Dashboard")
    }

    if ($realShortnames -contains "Hosting.Redis") {
        Test-Filtering -Name "RT2: Real projects - Hosting.Redis" `
            -AllTests $realShortnames `
            -ProjectsFilterJson '["tests/Aspire.Hosting.Redis.Tests/"]' `
            -ExpectedTests @("Hosting.Redis")
    }
}
else {
    Write-Host "Skipping real project tests (tests directory not found)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "==========================================="
$total = $script:Passed + $script:Failed
Write-Host "Total: $total | " -NoNewline
Write-Host "Passed: $($script:Passed)" -ForegroundColor Green -NoNewline
Write-Host " | " -NoNewline
Write-Host "Failed: $($script:Failed)" -ForegroundColor Red
Write-Host ""

if ($script:Failed -gt 0) {
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($failure in $script:Failures) {
        Write-Host "  - $failure"
    }
    exit 1
}

Write-Host "All tests passed!" -ForegroundColor Green
exit 0
