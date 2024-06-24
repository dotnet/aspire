#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates the code coverage policy for each project.
.DESCRIPTION
    This script compares code coverage with thresholds given in "MinCodeCoverage" property in each project.
    The script writes an error for each project that does not comply with the policy.
.PARAMETER CoberturaReportXml
    Path to the XML file to read the code coverage report from in Cobertura format
.EXAMPLE
    PS> .\ValidatePerProjectCoverage.ps1 -CoberturaReportXml .\Cobertura.xml
#>

param (
    [Parameter(Mandatory = $true, HelpMessage="Path to the XML file to read the code coverage report from")]
    [string]$CoberturaReportXml
)

function Write-Header {
    param($message, [bool]$isError);
    $color = if ($isError) { 'Red' } else { 'Green' };
    Write-Host $message -ForegroundColor $color;
    Write-Host ("=" * 80)
 }

 function Write-GitHubComment {
    param($message);
    $gitHubCommentVar = '##vso[task.setvariable variable=GITHUB_COMMENT]' + $message.Replace("`r`n","`n").Replace("`n","%0D%0A")
    Write-Host $gitHubCommentVar
 }

function Get-XmlValue { param($X, $Y); return $X.SelectSingleNode($Y).'#text' }

Write-Verbose "Reading cobertura report..."
[xml]$CoberturaReport = Get-Content $CoberturaReportXml
if ($null -eq $CoberturaReport.coverage -or 
    $null -eq $CoberturaReport.coverage.packages -or
    $null -eq $CoberturaReport.coverage.packages.package -or 
    0 -eq $CoberturaReport.coverage.packages.package.count)
{
    Write-GitHubComment -message ":bangbang: Code coverage information is missing or invalid."
    exit -1;
}

$ProjectToMinCoverageMap = @{}

Get-ChildItem -Path src -Include '*.*sproj' -Recurse | ForEach-Object {
    if ($_.FullName.Contains('Aspire.ProjectTemplates')) {
        return
    }

    $csProjXml = [xml](Get-Content $_)
    $AssemblyName = Get-XmlValue $csProjXml "//Project/PropertyGroup/AssemblyName"
    $MinCodeCoverage = Get-XmlValue $csProjXml "//Project/PropertyGroup/MinCodeCoverage"

    if ([string]::IsNullOrWhiteSpace($AssemblyName)) {
        $AssemblyName = $_.BaseName
    }

    if ([string]::IsNullOrWhiteSpace($MinCodeCoverage)) {
        # Test projects may not legitimely have min code coverage set.
        Write-Warning "$AssemblyName doesn't declare 'MinCodeCoverage' property or value is unset."
        return
    }

    $ProjectToMinCoverageMap[$AssemblyName] = $MinCodeCoverage
}

$esc = [char]27
$Errors = New-Object System.Collections.ArrayList
$Kudos = New-Object System.Collections.ArrayList
$ErrorsMarkdown = @();
$KudosMarkdown = @();

Write-Verbose "Collecting projects from code coverage report..."
$CoberturaReport.coverage.packages.package | ForEach-Object {
    $Name = $_.name

    # Code coverage information is in % (i.e., it's in fractions 0.123456) - bring it back to human readable format, e.g.: 12.34
    $LineCoverage = [math]::Round([double]$_.'line-rate' * 100, 2)
    $BranchCoverage = [math]::Round([double]$_.'branch-rate' * 100, 2)
    $IsFailed = $false

    Write-Verbose "Project $Name with line coverage $LineCoverage and branch coverage $BranchCoverage"

    if ($ProjectToMinCoverageMap.ContainsKey($Name)) {
        if ($ProjectToMinCoverageMap[$Name] -eq 'n/a')
        {
            Write-Host "$Name ...code coverage is not applicable"
            return
        }

        [double]$MinCodeCoverage = $ProjectToMinCoverageMap[$Name]

        # Since fractional math is somewhat unstable, for comparison purposes multiply our numbers by 100
        # and opearate on intergers. E.g., threshold 80 -> 8000, coverage 12.34 -> 1234.
        [int]$minCoverage100 = $MinCodeCoverage * 100;
        [int]$lineCoverage100 = $LineCoverage * 100;
        [int]$branchCoverage100 = $BranchCoverage * 100;

        # Detect the under-coverage
        if ($minCoverage100 -gt $lineCoverage100) {
            $IsFailed = $true
            $ErrorsMarkdown += "| $Name | Line | **$MinCodeCoverage** | $LineCoverage :small_red_triangle_down: |"
            [void]$Errors.Add(
                (
                    New-Object PSObject -Property @{
                        "Project" = $Name;
                        "Coverage Type" = "Line";
                        "Expected" = $MinCodeCoverage;
                        "Actual" = "$esc[1m$esc[0;31m$($LineCoverage)$esc[0m"
                    }
                )
            )
        }

        if ($minCoverage100 -gt $branchCoverage100) {
            $IsFailed = $true
            $ErrorsMarkdown += "| $Name | Branch | **$MinCodeCoverage** | $BranchCoverage :small_red_triangle_down: |"
            [void]$Errors.Add(
                (
                    New-Object PSObject -Property @{
                        "Project" = $Name;
                        "Coverage Type" = "Branch";
                        "Expected" = $MinCodeCoverage;
                        "Actual" = "$esc[1m$esc[0;31m$($BranchCoverage)$esc[0m"
                    }
                )
            )
        }

        # Detect the over-coverage
        # Pick the lesser value of two - line or branch coverage and then keep only the integer part of the number
        # because the threshold is denoted by an integer.
        # Attempts to round the coverage values to the nearest integer lead to a lot non-determinism and instability.
        [double]$lowestReported = [math]::Floor([math]::Min($LineCoverage, $BranchCoverage));
        Write-Verbose "$Name line: $LineCoverage, branch: $BranchCoverage, min: $lowestReported, threshold: $MinCodeCoverage"
        if ([int]$MinCodeCoverage -lt $lowestReported) {
            $KudosMarkdown += "| $Name | $MinCodeCoverage | **$lowestReported** |"
            [void]$Kudos.Add(
                (
                    New-Object PSObject -Property @{
                        "Project" = $Name;
                        "Expected" = $MinCodeCoverage;
                        "Actual" = "$esc[1m$esc[0;32m$($lowestReported)$esc[0m";
                    }
                )
            )
        }

        if ($IsFailed) { Write-Host "$Name" -NoNewline; Write-Host " ...failed validation" -ForegroundColor Red }
                  else { Write-Host "$Name" -NoNewline; Write-Host " ...ok" -ForegroundColor Green }
    }
    else {
        Write-Host "$Name ...skipping"
    }
}

if ($Kudos.Count -ne 0) {
    Write-Header -message "`r`nGood job! The coverage increased" -isError $false
    $Kudos | `
        Sort-Object Project | `
        Format-Table "Project", `
                    @{ Name="Expected"; Expression="Expected"; Width=10; Alignment = "Right" }, `
                    @{ Name="Actual"; Expression="Actual"; Width=10; Alignment = "Right" } `
                    -AutoSize -Wrap
    Write-Host "##vso[task.logissue type=warning;]Good job! The coverage increased, please update your projects"

    $sorted = $KudosMarkdown | Sort-Object;
    $KudosMarkdown = @(':tada: **Good job! The coverage increased** :tada:', 'Update `MinCodeCoverage` in the project files.', "`r`n", '| Project | Expected | Actual |', '| --- | ---: | ---: |', $sorted, "`r`n`r`n");
}

if ($Errors.Count -ne 0) {
    Write-Header -message "`r`n[!!] Found $($Errors.Count) issues!" -isError ($Errors.Count -ne 0)
    $Errors | `
        Sort-Object Project, 'Coverage Type' | `
        Format-Table "Project", `
                    @{ Name="Expected"; Expression="Expected"; Width=10; Alignment = "Right" }, `
                    @{ Name="Actual"; Expression="Actual"; Width=10; Alignment = "Right" }, `
                    @{ Name="Coverage Type"; Expression="Coverage Type"; Width=10; Alignment = "Center" } `
                    -AutoSize -Wrap

    $sorted = $ErrorsMarkdown | Sort-Object;
    $ErrorsMarkdown = @(":bangbang: **Found issues** :bangbang: ", "`r`n", '| Project | Coverage Type |Expected | Actual | ', '| --- | :---: | ---: | ---: |', $sorted, "`r`n`r`n");
}

# Write out markdown for publishing back to AzDO
'' | Out-File coverage-report.md -Encoding ascii
$ErrorsMarkdown | Out-File coverage-report.md -Encoding ascii -Append
$KudosMarkdown | Out-File coverage-report.md -Encoding ascii -Append

# Set the AzDO variable used by GitHubComment@0 task
[string]$markdown = Get-Content coverage-report.md -Raw
if (![string]::IsNullOrWhiteSpace($markdown)) {
    # Add link back to the Code Coverage board
    $link = "$($env:SYSTEM_COLLECTIONURI)$env:SYSTEM_TEAMPROJECT/_build/results?buildId=$env:BUILD_BUILDID&view=codecoverage-tab"
    $markdown = "$markdown`n`nFull code coverage report: $link"
    Write-GitHubComment -message $markdown
}

if ($Errors.Count -eq 0)
{
    Write-Host "`r`nAll good, no issues found."
    exit 0;
}

exit -1;
