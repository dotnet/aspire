<#
.SYNOPSIS
    Evaluates which test categories should run based on changed files.

.DESCRIPTION
    Analyzes git changes and determines which test categories need to run based on
    path matching rules defined in a JSON configuration file.

.PARAMETER DiffTarget
    Git ref to compare against (e.g., "HEAD~1", "origin/main...HEAD").
    Default is "HEAD~1".

.PARAMETER ConfigFile
    Path to the JSON configuration file.
    Default is "./test-selection-rules.json" relative to script location.

.PARAMETER OutputFile
    Optional path to write JSON output. If not specified, outputs to stdout.

.PARAMETER TestFiles
    Space-separated list of files for testing (overrides git diff).

.PARAMETER DryRun
    When set, outputs to console instead of GITHUB_OUTPUT.

.EXAMPLE
    ./Evaluate-TestSelection.ps1 -DiffTarget "origin/main...HEAD"

.EXAMPLE
    ./Evaluate-TestSelection.ps1 -TestFiles "src/Aspire.Dashboard/Foo.cs" -DryRun

.NOTES
    See docs/specs/test-selection-by-changed-paths.md for design details.
#>

[CmdletBinding()]
param(
    [string]$DiffTarget = "HEAD~1",
    [string]$ConfigFile,
    [string]$OutputFile,
    [string]$TestFiles,
    [switch]$DryRun
)

# Capture whether TestFiles was explicitly passed (before strict mode)
$script:UseTestFilesMode = $PSBoundParameters.ContainsKey('TestFiles')

# Set strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Default config file path
if (-not $ConfigFile) {
    $ConfigFile = Join-Path $PSScriptRoot "test-selection-rules.json"
}

#region Helper Functions

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Detail", "Verbose", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )

    switch ($Level) {
        "Info"    { Write-Host "=== $Message ===" }
        "Detail"  { Write-Host "  $Message" }
        "Verbose" { if ($VerbosePreference -eq "Continue") { Write-Host "    $Message" } }
        "Success" { Write-Host "  [OK] $Message" -ForegroundColor Green }
        "Warning" { Write-Host "  [WARN] $Message" -ForegroundColor Yellow }
        "Error"   { Write-Host "  [ERROR] $Message" -ForegroundColor Red }
    }
}

function Write-Output-Value {
    param(
        [string]$Key,
        [string]$Value
    )

    if ($DryRun) {
        # In dry-run mode, write to console for visibility
        Write-Host "$Key=$Value"
    }
    elseif ($env:GITHUB_OUTPUT) {
        "$Key=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    else {
        Write-Host "$Key=$Value"
    }
}

function Get-PropertyValue {
    <#
    .SYNOPSIS
        Safely gets a property value from an object, returning $null if not found.
    #>
    param(
        [object]$Object,
        [string]$PropertyName,
        $Default = $null
    )

    if ($null -eq $Object) { return $Default }
    if ($Object.PSObject.Properties.Name -contains $PropertyName) {
        return $Object.$PropertyName
    }
    return $Default
}

function Convert-GlobToRegex {
    <#
    .SYNOPSIS
        Converts a glob pattern to a regex pattern.
    .DESCRIPTION
        Handles: ** (any path including /), * (any segment not including /), ? (single char)
    #>
    param([string]$Pattern)

    $regex = ""
    $i = 0
    $len = $Pattern.Length

    while ($i -lt $len) {
        $char = $Pattern[$i]
        $nextChar = if ($i + 1 -lt $len) { $Pattern[$i + 1] } else { $null }

        switch ($char) {
            '*' {
                if ($nextChar -eq '*') {
                    # ** matches any path (including /)
                    $regex += ".*"
                    $i++
                }
                else {
                    # * matches any segment (not including /)
                    $regex += "[^/]*"
                }
            }
            '?' {
                # ? matches single char
                $regex += "."
            }
            { $_ -in '.', '[', ']', '^', '$', '(', ')', '{', '}', '|', '+', '\' } {
                # Escape special regex chars
                $regex += "\$char"
            }
            default {
                $regex += $char
            }
        }
        $i++
    }

    # Anchor the pattern
    return "^$regex$"
}

function Test-GlobMatch {
    <#
    .SYNOPSIS
        Tests if a file path matches a glob pattern.
    #>
    param(
        [string]$FilePath,
        [string]$Pattern
    )

    $regex = Convert-GlobToRegex -Pattern $Pattern
    return $FilePath -match $regex
}

function Convert-SourcePatternToRegex {
    <#
    .SYNOPSIS
        Converts a source pattern with {name} placeholder to a regex with capture group.
    .DESCRIPTION
        Handles glob patterns and captures the {name} portion as a named group.
    #>
    param([string]$Pattern)

    # First, escape special regex chars except * and ?
    $regex = ""
    $i = 0
    $len = $Pattern.Length

    while ($i -lt $len) {
        # Check for {name} placeholder
        if ($i + 5 -lt $len -and $Pattern.Substring($i, 6) -eq "{name}") {
            # Capture group for the name - match path segments (no slashes)
            $regex += "(?<name>[^/]+)"
            $i += 6
            continue
        }

        $char = $Pattern[$i]
        $nextChar = if ($i + 1 -lt $len) { $Pattern[$i + 1] } else { $null }

        switch ($char) {
            '*' {
                if ($nextChar -eq '*') {
                    $regex += ".*"
                    $i++
                }
                else {
                    $regex += "[^/]*"
                }
            }
            '?' {
                $regex += "."
            }
            { $_ -in '.', '[', ']', '^', '$', '(', ')', '{', '}', '|', '+', '\' } {
                $regex += "\$char"
            }
            default {
                $regex += $char
            }
        }
        $i++
    }

    return "^$regex$"
}

function Get-ProjectMappingMatch {
    <#
    .SYNOPSIS
        Checks if a file matches a project mapping and returns the test project path.
    .DESCRIPTION
        Returns $null if no match, or the resolved test project path if matched.
    #>
    param(
        [string]$FilePath,
        [object]$Mapping
    )

    $sourcePattern = $Mapping.sourcePattern
    $testPattern = $Mapping.testPattern
    $excludePatterns = Get-PropertyValue -Object $Mapping -PropertyName "exclude" -Default @()

    # Convert source pattern to regex with capture group
    $regex = Convert-SourcePatternToRegex -Pattern $sourcePattern

    if ($FilePath -match $regex) {
        $capturedName = $Matches['name']

        # Check exclusions
        foreach ($excludePattern in $excludePatterns) {
            if (Test-GlobMatch -FilePath $FilePath -Pattern $excludePattern) {
                Write-Log "  [projectMapping] $FilePath excluded by $excludePattern" -Level Verbose
                return $null
            }
        }

        # Substitute {name} in test pattern
        $testProject = $testPattern -replace '\{name\}', $capturedName
        Write-Log "  [projectMapping] $FilePath -> $testProject (via $sourcePattern)" -Level Verbose
        return $testProject
    }

    return $null
}

function Get-ProjectsFromMappings {
    <#
    .SYNOPSIS
        Gets all test projects for a list of files based on project mappings.
    .DESCRIPTION
        Returns an array of unique test project paths.
    #>
    param(
        [string[]]$Files,
        [array]$Mappings
    )

    $projects = @{}

    foreach ($file in $Files) {
        foreach ($mapping in $Mappings) {
            $testProject = Get-ProjectMappingMatch -FilePath $file -Mapping $mapping
            if ($null -ne $testProject -and -not $projects.ContainsKey($testProject)) {
                $projects[$testProject] = $true
            }
        }
    }

    return @($projects.Keys)
}

function Get-ChangedFiles {
    <#
    .SYNOPSIS
        Gets the list of changed files from git or test input.
    #>
    param(
        [string]$DiffTarget,
        [string]$TestFiles,
        [bool]$UseTestFiles = $false
    )

    if ($UseTestFiles) {
        # Test mode - use provided files (may be empty)
        # Use ,@() to prevent PowerShell from unrolling single-element arrays
        $files = @($TestFiles -split '\s+' | Where-Object { $_ -ne '' })
        return ,$files
    }

    # Git diff mode
    try {
        $output = git diff --name-only $DiffTarget 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Git diff failed: $output"
        }
        $files = @($output -split "`n" | Where-Object { $_ -ne '' })
        return ,$files
    }
    catch {
        Write-Log "Git diff failed: $_" -Level Error
        return $null
    }
}

function Get-IgnoredAndActiveFiles {
    <#
    .SYNOPSIS
        Splits files into ignored and active lists based on ignore patterns.
    #>
    param(
        [string[]]$Files,
        [string[]]$IgnorePatterns
    )

    $ignored = @()
    $active = @()

    foreach ($file in $Files) {
        $isIgnored = $false
        foreach ($pattern in $IgnorePatterns) {
            if (Test-GlobMatch -FilePath $file -Pattern $pattern) {
                $isIgnored = $true
                break
            }
        }
        if ($isIgnored) {
            $ignored += $file
        } else {
            $active += $file
        }
    }

    return @{
        Ignored = $ignored
        Active = $active
    }
}

#endregion

#region Main Logic

function Invoke-TestSelection {
    Write-Log "Test Selection Evaluator"
    Write-Log "Config: $ConfigFile" -Level Detail

    # Load configuration
    if (-not (Test-Path $ConfigFile)) {
        Write-Log "Config file not found: $ConfigFile" -Level Error
        return @{
            run_all = $true
            trigger_reason = "fallback"
            error = "Config file not found"
            changed_files = @()
            ignored_files = @()
            categories = @{}
            projects = @()
        }
    }

    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json

    # Get changed files
    if ($script:UseTestFilesMode) {
        Write-Log "Mode: test files" -Level Detail
    }
    else {
        Write-Log "DiffTarget: $DiffTarget" -Level Detail
    }

    $changedFiles = Get-ChangedFiles -DiffTarget $DiffTarget -TestFiles $TestFiles -UseTestFiles $script:UseTestFilesMode

    if ($null -eq $changedFiles) {
        # Git diff failed - fallback
        Write-Log "Changed Files (error)" -Level Info
        Write-Log "Git diff failed - running ALL tests" -Level Detail

        return @{
            run_all = $true
            trigger_reason = "fallback"
            error = "Git diff failed"
            changed_files = @()
            ignored_files = @()
            categories = @{}
            projects = @()
        }
    }

    $fileCount = $changedFiles.Count
    Write-Log "Changed Files ($fileCount)"

    if ($fileCount -eq 0) {
        Write-Log "No files changed" -Level Detail

        # Initialize all categories as disabled
        $categoryResults = @{}
        foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
            $categoryResults[$categoryName] = @{
                enabled = $false
                reason = "no files changed"
            }
        }

        return @{
            run_all = $false
            trigger_reason = "no_changes"
            changed_files = @()
            ignored_files = @()
            categories = $categoryResults
            projects = @()
        }
    }

    # Display changed files (first 20)
    $displayFiles = $changedFiles | Select-Object -First 20
    foreach ($file in $displayFiles) {
        Write-Log $file -Level Detail
    }
    if ($fileCount -gt 20) {
        Write-Log "... and $($fileCount - 20) more" -Level Detail
    }
    Write-Host ""

    # Filter out ignored files
    $ignorePaths = Get-PropertyValue -Object $config -PropertyName "ignorePaths" -Default @()
    $splitResult = Get-IgnoredAndActiveFiles -Files $changedFiles -IgnorePatterns $ignorePaths
    $ignoredFiles = $splitResult.Ignored
    $activeFiles = $splitResult.Active

    if ($ignoredFiles.Count -gt 0) {
        Write-Log "Ignored Files ($($ignoredFiles.Count))"
        foreach ($file in $ignoredFiles) {
            Write-Log "[IGNORED] $file" -Level Detail
        }
        Write-Host ""
    }

    # If all files are ignored, no tests need to run
    if ($activeFiles.Count -eq 0) {
        Write-Log "All files are ignored - no tests to run" -Level Detail

        # Initialize all categories as disabled
        $categoryResults = @{}
        foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
            $categoryResults[$categoryName] = @{
                enabled = $false
                reason = "all files ignored"
            }
        }

        return @{
            run_all = $false
            trigger_reason = "all_ignored"
            changed_files = @()
            ignored_files = $ignoredFiles
            categories = $categoryResults
            projects = @()
        }
    }

    # Initialize tracking
    $categoryResults = @{}
    $triggeredCategories = @{}
    $matchedFiles = @{}
    $allProjects = @()

    foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
        $categoryResults[$categoryName] = @{
            enabled = $false
            reason = "no matching changes"
        }
        $triggeredCategories[$categoryName] = $false
    }

    # Check for triggerAll patterns first
    Write-Log "Checking TriggerAll Patterns"

    foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
        $category = $config.categories.$categoryName

        $isTriggerAll = Get-PropertyValue -Object $category -PropertyName "triggerAll" -Default $false
        $triggerPaths = Get-PropertyValue -Object $category -PropertyName "triggerPaths" -Default @()

        if ($isTriggerAll -eq $true -and $triggerPaths.Count -gt 0) {
            foreach ($file in $activeFiles) {
                foreach ($pattern in $category.triggerPaths) {
                    if (Test-GlobMatch -FilePath $file -Pattern $pattern) {
                        Write-Log "MATCH: $file -> $pattern (triggerAll)" -Level Detail
                        Write-Log "Result: TriggerAll matched - running ALL tests" -Level Detail
                        Write-Host ""

                        # Mark all categories as enabled
                        foreach ($catName in $config.categories.PSObject.Properties.Name) {
                            $categoryResults[$catName] = @{
                                enabled = $true
                                reason = "triggerAll: $categoryName matched $pattern"
                            }
                        }

                        return @{
                            run_all = $true
                            trigger_reason = "critical_path"
                            trigger_category = $categoryName
                            trigger_pattern = $pattern
                            trigger_file = $file
                            changed_files = $activeFiles
                            ignored_files = $ignoredFiles
                            categories = $categoryResults
                            projects = @()
                        }
                    }
                }
            }
        }
    }

    Write-Log "No triggerAll patterns matched" -Level Detail
    Write-Host ""

    # Evaluate each category
    Write-Log "Evaluating Categories"

    foreach ($file in $activeFiles) {
        $fileMatched = $false

        foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
            $category = $config.categories.$categoryName

            # Skip triggerAll categories (already handled)
            $isTriggerAll = Get-PropertyValue -Object $category -PropertyName "triggerAll" -Default $false
            if ($isTriggerAll -eq $true) {
                continue
            }

            $matchesInclude = $false
            $matchesExclude = $false
            $matchedPattern = ""

            # Check include patterns
            $catTriggerPaths = Get-PropertyValue -Object $category -PropertyName "triggerPaths" -Default @()
            foreach ($pattern in $catTriggerPaths) {
                if (Test-GlobMatch -FilePath $file -Pattern $pattern) {
                    $matchesInclude = $true
                    $matchedPattern = $pattern
                    break
                }
            }

            if ($matchesInclude) {
                # Check exclude patterns
                $catExcludePaths = Get-PropertyValue -Object $category -PropertyName "excludePaths" -Default @()
                foreach ($pattern in $catExcludePaths) {
                    if (Test-GlobMatch -FilePath $file -Pattern $pattern) {
                        $matchesExclude = $true
                        break
                    }
                }

                if (-not $matchesExclude) {
                    Write-Log "[$categoryName] Matched: $file" -Level Detail
                    $triggeredCategories[$categoryName] = $true
                    $categoryResults[$categoryName] = @{
                        enabled = $true
                        reason = "matched: $matchedPattern"
                    }
                    $fileMatched = $true

                    # Collect projects
                    $catProjects = Get-PropertyValue -Object $category -PropertyName "projects" -Default @()
                    foreach ($project in $catProjects) {
                        if ($project -notin $allProjects) {
                            $allProjects += $project
                        }
                    }
                }
                else {
                    Write-Log "[$categoryName] Excluded: $file" -Level Verbose
                }
            }
        }

        if ($fileMatched) {
            $matchedFiles[$file] = $true
        }
    }

    Write-Host ""

    # Check for unmatched files (conservative fallback)
    Write-Log "Unmatched Files Check"
    $hasUnmatched = $false
    $unmatchedFiles = @()

    foreach ($file in $activeFiles) {
        if (-not $matchedFiles.ContainsKey($file)) {
            Write-Log "Unmatched: $file" -Level Detail
            $hasUnmatched = $true
            $unmatchedFiles += $file
        }
    }

    if ($hasUnmatched) {
        Write-Log "Result: Unmatched files found - running ALL tests (conservative)" -Level Detail
        Write-Host ""

        # Mark all categories as enabled
        foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
            $categoryResults[$categoryName] = @{
                enabled = $true
                reason = "conservative fallback: unmatched files"
            }
        }

        return @{
            run_all = $true
            trigger_reason = "conservative_fallback"
            unmatched_files = $unmatchedFiles
            changed_files = $activeFiles
            ignored_files = $ignoredFiles
            categories = $categoryResults
            projects = @()
        }
    }

    Write-Log "Result: All files matched at least one category" -Level Detail
    Write-Host ""

    # Apply project mappings to get specific test projects
    $projectMappings = Get-PropertyValue -Object $config -PropertyName "projectMappings" -Default @()
    if ($projectMappings.Count -gt 0) {
        Write-Log "Applying Project Mappings"
        $mappedProjects = Get-ProjectsFromMappings -Files $activeFiles -Mappings $projectMappings

        foreach ($project in $mappedProjects) {
            Write-Log "Mapped project: $project" -Level Detail
            if ($project -notin $allProjects) {
                $allProjects += $project
            }
        }
        Write-Host ""
    }

    return @{
        run_all = $false
        trigger_reason = "normal"
        changed_files = $activeFiles
        ignored_files = $ignoredFiles
        categories = $categoryResults
        projects = $allProjects
    }
}

#endregion

#region Main Execution

try {
    $result = Invoke-TestSelection

    # Output summary
    Write-Log "Summary"

    # Write individual outputs for workflow
    Write-Output-Value "run_all" $result.run_all.ToString().ToLower()

    # Get category names from config (if available) to ensure consistent output
    if (Test-Path $ConfigFile) {
        $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
        foreach ($categoryName in $config.categories.PSObject.Properties.Name) {
            $enabled = if ($result.categories.$categoryName.enabled) { "true" } else { "false" }
            Write-Output-Value "run_$categoryName" $enabled
        }
    }

    # Output specific projects for filtering (only for integrations)
    $projectsJson = if ($result.projects.Count -gt 0) {
        ConvertTo-Json $result.projects -Compress
    } else {
        "[]"
    }
    Write-Output-Value "integrations_projects" $projectsJson

    # Output JSON to stdout (for debugging and optional capture)
    $jsonOutput = $result | ConvertTo-Json -Depth 10

    if ($OutputFile) {
        $jsonOutput | Out-File -FilePath $OutputFile -Encoding utf8
        Write-Log "JSON output written to: $OutputFile" -Level Detail
    }

    # Return JSON for pipeline capture
    Write-Output $jsonOutput

    exit 0
}
catch {
    Write-Log "Error: $($_.Exception.Message)" -Level Error
    Write-Log $_.ScriptStackTrace -Level Error

    # Fallback - run all tests
    Write-Output-Value "run_all" "true"

    $errorResult = @{
        run_all = $true
        trigger_reason = "error"
        error = $_.Exception.Message
        changed_files = @()
        ignored_files = @()
        categories = @{}
        projects = @()
    } | ConvertTo-Json -Depth 10

    Write-Output $errorResult

    exit 1
}

#endregion
