<#
.SYNOPSIS
    Test harness for Evaluate-TestSelection.ps1

.DESCRIPTION
    Runs all test cases from docs/specs/test-selection-by-changed-paths.md
    Exit 0 if all pass, exit 1 with failures listed

.EXAMPLE
    ./Test-EvaluateTestSelection.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$EvaluateScript = Join-Path $RepoRoot "eng/scripts/Evaluate-TestSelection.ps1"
$ConfigFile = Join-Path $RepoRoot "eng/scripts/test-selection-rules.json"

# Test tracking
$script:Passed = 0
$script:Failed = 0
$script:Failures = @()

function Write-TestHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "=== $Message ===" -ForegroundColor Blue
}

function Invoke-Test {
    <#
    .SYNOPSIS
        Runs a single test case
    .PARAMETER Name
        Test name
    .PARAMETER Files
        Space-separated list of files
    .PARAMETER Expected
        Hashtable of expected category states
    .PARAMETER ExpectedProjects
        Optional array of expected project paths
    #>
    param(
        [string]$Name,
        [string]$Files,
        [hashtable]$Expected,
        [string[]]$ExpectedProjects = @()
    )

    # Run the evaluate script and capture JSON output
    $jsonOutput = & $EvaluateScript -ConfigFile $ConfigFile -TestFiles $Files -DryRun 2>$null

    # Parse JSON output
    $results = @{}
    $actualProjects = @()
    try {
        $json = $jsonOutput | ConvertFrom-Json
        $results["run_all"] = $json.run_all.ToString().ToLower()

        # Extract category results
        foreach ($catName in $json.categories.PSObject.Properties.Name) {
            $enabled = $json.categories.$catName.enabled
            $results["run_$catName"] = $enabled.ToString().ToLower()
        }

        # Extract projects
        if ($null -ne $json.projects) {
            $actualProjects = @($json.projects)
        }
    }
    catch {
        Write-Host "  Failed to parse JSON: $_" -ForegroundColor Yellow
        Write-Host "  Output: $jsonOutput" -ForegroundColor Yellow
    }

    # Check results
    $pass = $true
    $details = @()

    foreach ($key in $Expected.Keys) {
        $expectedValue = $Expected[$key].ToString().ToLower()
        $gotValue = if ($results.ContainsKey($key)) { $results[$key] } else { "missing" }

        if ($gotValue -ne $expectedValue) {
            $pass = $false
            $details += "${key}: got $gotValue, expected $expectedValue"
        }
    }

    # Check expected projects if specified
    if ($ExpectedProjects.Count -gt 0) {
        foreach ($expectedProject in $ExpectedProjects) {
            if ($expectedProject -notin $actualProjects) {
                $pass = $false
                $details += "missing project: $expectedProject"
            }
        }
    }

    if ($pass) {
        Write-Host "PASS $Name" -ForegroundColor Green
        $script:Passed++
    }
    else {
        Write-Host "FAIL $Name" -ForegroundColor Red
        Write-Host "     Files: $Files"
        foreach ($detail in $details) {
            Write-Host "     $detail"
        }
        if ($ExpectedProjects.Count -gt 0) {
            Write-Host "     actual projects: $($actualProjects -join ', ')"
        }
        $script:Failed++
        $script:Failures += $Name
    }
}

function Invoke-AllTest {
    <#
    .SYNOPSIS
        Test that expects all categories to be true (run_all=true)
    #>
    param(
        [string]$Name,
        [string]$Files
    )

    Invoke-Test -Name $Name -Files $Files -Expected @{
        "run_all" = "true"
        "run_templates" = "true"
        "run_cli_e2e" = "true"
        "run_endtoend" = "true"
        "run_integrations" = "true"
        "run_extension" = "true"
    }
}

function Invoke-NoneTest {
    <#
    .SYNOPSIS
        Test that expects all categories to be false
    #>
    param(
        [string]$Name,
        [string]$Files
    )

    Invoke-Test -Name $Name -Files $Files -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }
}

# Main test execution
Write-Host "=== Test Harness for Evaluate-TestSelection.ps1 ==="
Write-Host "Config: $ConfigFile"

Write-TestHeader "Fallback Tests"

# F1: eng/ is in ignorePaths, so no tests run
Invoke-NoneTest -Name "F1: eng ignored" `
    -Files "eng/Version.Details.xml"

Invoke-AllTest -Name "F2: Directory.Build.props" `
    -Files "Directory.Build.props"

# F3: .github/workflows/ is in ignorePaths, so no tests run
Invoke-NoneTest -Name "F3: workflow ignored" `
    -Files ".github/workflows/ci.yml"

Invoke-AllTest -Name "F4: tests/Shared fallback" `
    -Files "tests/Shared/TestHelper.cs"

Invoke-AllTest -Name "F5: global.json" `
    -Files "global.json"

Invoke-AllTest -Name "F6: Aspire.slnx" `
    -Files "Aspire.slnx"

Write-TestHeader "Category: templates"

Invoke-Test -Name "T1: Template source" `
    -Files "src/Aspire.ProjectTemplates/templates/aspire-starter/Program.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "true"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "T2: Template test" `
    -Files "tests/Aspire.Templates.Tests/TemplateTests.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "true"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Write-TestHeader "Category: cli_e2e"

Invoke-Test -Name "C1: CLI source" `
    -Files "src/Aspire.Cli/Commands/NewCommand.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "true"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "C2: CLI E2E test" `
    -Files "tests/Aspire.Cli.EndToEnd.Tests/NewCommandTests.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "true"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "C3: CLI E2E test nested" `
    -Files "tests/Aspire.Cli.EndToEnd.Tests/Commands/SomeTest.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "true"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Write-TestHeader "Category: endtoend"

Invoke-Test -Name "E1: EndToEnd test" `
    -Files "tests/Aspire.EndToEnd.Tests/SomeTest.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "true"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "E2: Playground change" `
    -Files "playground/TestShop/TestShop.AppHost/Program.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "true"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Write-TestHeader "Category: integrations"

Invoke-Test -Name "I1: Dashboard component" `
    -Files "src/Aspire.Dashboard/Components/Layout.razor" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

# I2: src/Aspire.Hosting/** is in core triggerAll, so runs all tests
Invoke-AllTest -Name "I2: Hosting source (triggerAll)" `
    -Files "src/Aspire.Hosting/ApplicationModel/Resource.cs"

Invoke-Test -Name "I3: Dashboard test" `
    -Files "tests/Aspire.Dashboard.Tests/DashboardTests.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

Invoke-Test -Name "I4: Azure extension" `
    -Files "src/Aspire.Hosting.Azure/AzureExtensions.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

Invoke-Test -Name "I5: Components source" `
    -Files "src/Components/SomeComponent.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

Invoke-Test -Name "I6: Shared source" `
    -Files "src/Shared/Utils.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

Write-TestHeader "Category: extension"

Invoke-Test -Name "X1: extension package.json" `
    -Files "extension/package.json" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "true"
    }

Invoke-Test -Name "X2: extension source" `
    -Files "extension/src/extension.ts" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "true"
    }

Write-TestHeader "Multi-Category Tests"

Invoke-Test -Name "M1: Dashboard + Extension" `
    -Files "src/Aspire.Dashboard/Foo.cs extension/bar.ts" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "true"
    }

Invoke-Test -Name "M2: CLI + Dashboard" `
    -Files "src/Aspire.Cli/Cmd.cs src/Aspire.Dashboard/Foo.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "true"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

Invoke-Test -Name "M3: Templates + Playground" `
    -Files "src/Aspire.ProjectTemplates/X.cs playground/Y.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "true"
        "run_cli_e2e" = "false"
        "run_endtoend" = "true"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Write-TestHeader "Ignored Files Tests"

# These are in ignorePaths, so no tests run
Invoke-NoneTest -Name "IG1: README.md ignored" `
    -Files "README.md"

Invoke-NoneTest -Name "IG2: docs folder ignored" `
    -Files "docs/getting-started.md"

Invoke-NoneTest -Name "IG3: .gitignore ignored" `
    -Files ".gitignore"

Write-TestHeader "Conservative Fallback Tests"

# Files not in ignorePaths and not matching any category trigger fallback
Invoke-AllTest -Name "U1: random file" `
    -Files "some-random-file.txt"

Invoke-AllTest -Name "U2: unknown src file" `
    -Files "src/Unknown/Something.cs"

Write-TestHeader "Edge Cases"

Invoke-Test -Name "EC1: README in templates dir" `
    -Files "src/Aspire.ProjectTemplates/README.md" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "true"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "EC2: README in CLI E2E dir" `
    -Files "tests/Aspire.Cli.EndToEnd.Tests/README.md" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "true"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "EC3: New Aspire.* project (not excluded)" `
    -Files "src/Aspire.Cli.SomeNew/Foo.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "true"
        "run_extension" = "false"
    }

# EC4: No changes
Write-Host -NoNewline "EC4: No changes... "
$output = & $EvaluateScript -ConfigFile $ConfigFile -TestFiles "" -DryRun 2>&1 | Out-String
if ($output -match "No files changed" -or $output -match "run_all=false") {
    Write-Host "PASS" -ForegroundColor Green
    $script:Passed++
}
else {
    Write-Host "FAIL" -ForegroundColor Red
    $script:Failed++
    $script:Failures += "EC4: No changes"
}

Write-TestHeader "Exclude Pattern Tests"

Invoke-Test -Name "EX1: Templates excluded from integrations" `
    -Files "src/Aspire.ProjectTemplates/Foo.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "true"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "EX2: CLI excluded from integrations" `
    -Files "src/Aspire.Cli/Bar.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "false"
        "run_cli_e2e" = "true"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Invoke-Test -Name "EX3: Template tests excluded from integrations" `
    -Files "tests/Aspire.Templates.Tests/X.cs" `
    -Expected @{
        "run_all" = "false"
        "run_templates" = "true"
        "run_cli_e2e" = "false"
        "run_endtoend" = "false"
        "run_integrations" = "false"
        "run_extension" = "false"
    }

Write-TestHeader "Project Mapping Tests"

Invoke-Test -Name "PM1: Components mapping" `
    -Files "src/Components/Aspire.Microsoft.Data.SqlClient/SqlClientExtensions.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @("tests/Aspire.Microsoft.Data.SqlClient.Tests/")

Invoke-Test -Name "PM2: Aspire.Hosting.X mapping" `
    -Files "src/Aspire.Hosting.Redis/RedisBuilderExtensions.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @("tests/Aspire.Hosting.Redis.Tests/")

Invoke-Test -Name "PM3: Aspire.Hosting.Testing excluded from mapping" `
    -Files "src/Aspire.Hosting.Testing/DistributedApplicationTestingBuilder.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @()

Invoke-Test -Name "PM4: Test project self-mapping" `
    -Files "tests/Aspire.Dashboard.Tests/DashboardTests.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @("tests/Aspire.Dashboard.Tests/")

Invoke-Test -Name "PM5: Multiple files, multiple mappings" `
    -Files "src/Components/Aspire.Npgsql/NpgsqlExtensions.cs src/Aspire.Hosting.PostgreSQL/PostgreSQLExtensions.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @("tests/Aspire.Npgsql.Tests/", "tests/Aspire.Hosting.PostgreSQL.Tests/")

Invoke-Test -Name "PM6: Aspire.Hosting.Azure mapping" `
    -Files "src/Aspire.Hosting.Azure/AzureExtensions.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @("tests/Aspire.Hosting.Azure.Tests/")

Invoke-Test -Name "PM7: Nested path in Aspire.Hosting.X" `
    -Files "src/Aspire.Hosting.Milvus/MilvusBuilderExtensions.cs" `
    -Expected @{
        "run_all" = "false"
        "run_integrations" = "true"
    } `
    -ExpectedProjects @("tests/Aspire.Hosting.Milvus.Tests/")

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
