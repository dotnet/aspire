// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.TestSelector;
using Aspire.TestSelector.Analyzers;
using Aspire.TestSelector.Models;

// Define CLI options
var solutionOption = new Option<string>("--solution", "-s") { Description = "Path to the solution file (.sln or .slnx)", Required = true };
var configOption = new Option<string?>("--config", "-c") { Description = "Path to the test selector configuration file (if not provided, category logic is skipped)" };
var fromOption = new Option<string?>("--from", "-f") { Description = "Git ref to compare from (e.g., origin/main). Required unless --changed-files is provided" };
var toOption = new Option<string?>("--to", "-t") { Description = "Git ref to compare to (default: HEAD)" };
var changedFilesOption = new Option<string?>("--changed-files") { Description = "Comma-separated list of changed files (bypasses git entirely)" };
var outputOption = new Option<string?>("--output", "-o") { Description = "Output file path for the JSON result" };
var githubOutputOption = new Option<bool>("--github-output") { Description = "Output in GitHub Actions format" };
var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };

var rootCommand = new RootCommand("Test selection tool for Aspire")
{
    solutionOption,
    configOption,
    fromOption,
    toOption,
    changedFilesOption,
    outputOption,
    githubOutputOption,
    verboseOption
};

rootCommand.SetAction(async result =>
{
    var solution = result.GetValue(solutionOption)!;
    var configPath = result.GetValue(configOption);
    var fromRef = result.GetValue(fromOption);
    var toRef = result.GetValue(toOption);
    var changedFilesStr = result.GetValue(changedFilesOption);
    var outputPath = result.GetValue(outputOption);
    var githubOutput = result.GetValue(githubOutputOption);
    var verbose = result.GetValue(verboseOption);

    var workingDir = Directory.GetCurrentDirectory();

    // Detect CI environment for appropriate output formatting
    var ciEnvironment = DetectCIEnvironment();

    // Validate: --from is required unless --changed-files is provided
    if (string.IsNullOrEmpty(changedFilesStr) && string.IsNullOrEmpty(fromRef))
    {
        WriteError(ciEnvironment, "--from is required when --changed-files is not provided");
        Environment.Exit(1);
        return;
    }

    if (verbose)
    {
        Console.WriteLine($"Working directory: {workingDir}");
        Console.WriteLine($"CI environment: {ciEnvironment}");
        Console.WriteLine($"Solution: {solution}");
        Console.WriteLine($"Config: {configPath ?? "(none - category logic skipped)"}");
        if (!string.IsNullOrEmpty(changedFilesStr))
        {
            Console.WriteLine("Changed files: (provided via --changed-files)");
        }
        else
        {
            Console.WriteLine($"From ref: {fromRef}");
            Console.WriteLine($"To ref: {toRef ?? "HEAD"}");
        }
    }

    try
    {
        // Load configuration (optional - if not provided, category logic is skipped)
        TestSelectorConfig? config = null;
        if (!string.IsNullOrEmpty(configPath))
        {
            var configFullPath = Path.IsPathRooted(configPath) ? configPath : Path.Combine(workingDir, configPath);
            if (!File.Exists(configFullPath))
            {
                WriteError(ciEnvironment, $"Configuration file not found: {configFullPath}");
                Environment.Exit(1);
                return;
            }

            config = TestSelectorConfig.LoadFromFile(configFullPath);
            if (verbose)
            {
                Console.WriteLine($"Loaded config with {config.IgnorePaths.Count} ignore patterns");
            }
        }
        else if (verbose)
        {
            Console.WriteLine("No config file provided - using dotnet-affected only (category logic skipped)");
        }

        // Get changed files
        List<string> changedFiles;
        if (!string.IsNullOrEmpty(changedFilesStr))
        {
            changedFiles = changedFilesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToList();

            if (verbose)
            {
                Console.WriteLine($"Using {changedFiles.Count} explicitly provided changed files");
            }
        }
        else
        {
            // fromRef is guaranteed non-null here due to earlier validation
            changedFiles = GetGitChangedFiles(fromRef!, toRef, workingDir);

            if (verbose)
            {
                Console.WriteLine($"Found {changedFiles.Count} changed files from git diff");
            }
        }

        // Run the evaluation
        var evaluationResult = await EvaluateAsync(config, changedFiles, solution, fromRef, toRef, workingDir, ciEnvironment, verbose).ConfigureAwait(false);

        // Output result
        if (githubOutput)
        {
            evaluationResult.WriteGitHubOutput();
        }
        else
        {
            var json = evaluationResult.ToJson();

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputFullPath = Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(workingDir, outputPath);
                File.WriteAllText(outputFullPath, json);
                Console.WriteLine($"Result written to {outputFullPath}");
            }
            else
            {
                Console.WriteLine(json);
            }
        }
    }
    catch (Exception ex)
    {
        WriteError(ciEnvironment, $"Test selector failed: {ex.Message}");
        var errorResult = TestSelectionResult.WithError(ex.Message);
        if (githubOutput)
        {
            errorResult.WriteGitHubOutput();
        }
        else
        {
            Console.WriteLine(errorResult.ToJson());
        }
        Environment.Exit(1);
    }
});

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

static List<string> GetGitChangedFiles(string fromRef, string? toRef, string workingDir)
{
    var args = new List<string> { "diff", "--name-only", fromRef };
    if (!string.IsNullOrEmpty(toRef))
    {
        args.Add(toRef);
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = "git",
        WorkingDirectory = workingDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    foreach (var arg in args)
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo);
    if (process is null)
    {
        throw new InvalidOperationException("Failed to start git process");
    }

    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        var error = process.StandardError.ReadToEnd();
        throw new InvalidOperationException($"git diff failed: {error}");
    }

    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(f => f.Trim())
        .ToList();
}

// Evaluates changed files and determines which tests to run.
//
// Evaluation Flow:
// ================
//
//                    ┌─────────────────┐
//                    │  Changed Files  │
//                    └────────┬────────┘
//                             │
//                    ┌────────▼────────┐
//                    │ Filter Ignored  │
//                    │     Files       │
//                    └────────┬────────┘
//                             │
//              ┌──────────────┼──────────────┐
//              │ all ignored  │              │
//              ▼              │              │
//     ┌────────────────┐     │              │
//     │  Skip Tests    │     │              │
//     └────────────────┘     │              │
//                            │              │
//              ┌─────────────▼──────────────┐
//              │  Check TriggerAll Paths    │
//              └─────────────┬──────────────┘
//                            │
//              ┌─────────────┼──────────────┐
//              │ matched     │              │
//              ▼             │              │
//     ┌────────────────┐    │              │
//     │  Run ALL Tests │    │              │
//     └────────────────┘    │              │
//                           │              │
//           ┌───────────────▼──────────────┐
//           │  With Config? (categories,   │
//           │  sourceToTestMappings, etc.) │
//           └───────────────┬──────────────┘
//                           │
//          ┌────────────────┼─────────────────┐
//          │ no config      │  has config     │
//          ▼                ▼                 │
//   ┌──────────────┐  ┌──────────────────┐   │
//   │ dotnet-      │  │ Match Categories │   │
//   │ affected     │  │ + SourceToTest   │   │
//   │ only         │  │ Mappings         │   │
//   └──────┬───────┘  └────────┬─────────┘   │
//          │                   │             │
//          │          ┌────────▼─────────┐   │
//          │          │ Check Unmatched  │   │
//          │          │ Files            │   │
//          │          └────────┬─────────┘   │
//          │                   │             │
//          │    ┌──────────────┼─────────────┤
//          │    │ unmatched    │             │
//          │    ▼              │             │
//          │  ┌──────────────┐ │             │
//          │  │ Run ALL      │ │             │
//          │  │ Tests        │ │             │
//          │  └──────────────┘ │             │
//          │                   │             │
//          │          ┌────────▼─────────┐   │
//          │          │  dotnet-affected │   │
//          │          └────────┬─────────┘   │
//          │                   │             │
//          │    ┌──────────────┼─────────────┤
//          │    │ failed       │             │
//          │    ▼              │             │
//          │  ┌──────────────┐ │             │
//          │  │ Run ALL      │ │             │
//          │  │ Tests        │ │             │
//          │  └──────────────┘ │             │
//          │                   │             │
//          │          ┌────────▼─────────┐   │
//          │          │ Filter + Combine │   │
//          │          │ Test Projects    │   │
//          └──────────►────────┬─────────┘   │
//                              │             │
//                     ┌────────▼─────────┐   │
//                     │ Build Result     │   │
//                     │ (selective run)  │   │
//                     └──────────────────┘
static async Task<TestSelectionResult> EvaluateAsync(
    TestSelectorConfig? config,
    List<string> changedFiles,
    string solution,
    string? fromRef,
    string? toRef,
    string workingDir,
    string ciEnvironment,
    bool verbose)
{
    var logger = new DiagnosticLogger(verbose);

    // Step 1: Handle no changes
    if (changedFiles.Count == 0)
    {
        return HandleNoChanges(logger, config);
    }

    logger.LogInfo($"Processing {changedFiles.Count} changed files");

    // Step 2: Filter ignored files
    var (activeFiles, ignoredFiles) = FilterIgnoredFiles(logger, config, changedFiles);

    if (activeFiles.Count == 0)
    {
        return HandleAllFilesIgnored(logger, config, ignoredFiles);
    }

    // Step 3: Check for triggerAll paths
    var triggerAllResult = CheckTriggerAllPaths(logger, config, activeFiles, ignoredFiles);
    if (triggerAllResult is not null)
    {
        return triggerAllResult;
    }

    // Step 4: If no config, use dotnet-affected only (skip category logic)
    if (config is null)
    {
        return await EvaluateWithDotnetAffectedOnlyAsync(
            logger, activeFiles, ignoredFiles, solution, fromRef, toRef, workingDir, ciEnvironment, verbose).ConfigureAwait(false);
    }

    // Step 5: Match files to categories via triggerPaths
    var (pathTriggeredCategories, pathMatchedFiles) = MatchFilesToCategories(logger, config, activeFiles);

    // Step 6: Apply sourceToTestMappings
    var (mappedProjects, mappingMatchedFiles) = ApplySourceToTestMappings(logger, config, activeFiles);

    // Step 7: Check for unmatched files (conservative fallback)
    var unmatchedResult = CheckUnmatchedFiles(
        logger, config, activeFiles, ignoredFiles, pathMatchedFiles, mappingMatchedFiles);
    if (unmatchedResult is not null)
    {
        return unmatchedResult;
    }

    // Step 8: Run dotnet-affected
    var affectedResult = await RunDotnetAffectedAsync(
        logger, config, activeFiles, ignoredFiles, solution, fromRef, toRef, workingDir, ciEnvironment, verbose).ConfigureAwait(false);
    if (affectedResult.FallbackResult is not null)
    {
        return affectedResult.FallbackResult;
    }

    // Step 9: Filter and combine test projects
    var allTestProjects = FilterAndCombineTestProjects(
        logger, config, affectedResult.AffectedProjects, mappedProjects);

    // Step 10: Match test projects against categories
    UpdateCategoriesFromTestProjects(logger, config, pathTriggeredCategories, allTestProjects);

    // Step 11: Build final result
    return BuildSelectiveResult(
        logger, activeFiles, ignoredFiles, affectedResult.AffectedProjects, allTestProjects, pathTriggeredCategories);
}

static TestSelectionResult HandleNoChanges(DiagnosticLogger logger, TestSelectorConfig? config)
{
    logger.LogInfo("No changed files detected");
    var result = TestSelectionResult.NoChanges();
    InitializeCategories(result, config);
    logger.LogSummary(false, "no_changes", 0, []);
    return result;
}

static (List<string> ActiveFiles, List<string> IgnoredFiles) FilterIgnoredFiles(
    DiagnosticLogger logger,
    TestSelectorConfig? config,
    List<string> changedFiles)
{
    logger.LogStep("Filter Ignored Files");

    var ignorePatterns = config?.IgnorePaths ?? [];
    var ignoreFilter = new IgnorePathFilter(ignorePatterns);
    var ignoreResult = ignoreFilter.SplitFilesWithDetails(changedFiles);

    logger.LogInfo($"Ignore patterns configured: {ignoreFilter.Patterns.Count}");
    if (ignoreResult.IgnoredFiles.Count > 0)
    {
        logger.LogSubSection("Ignored files");
        foreach (var ignored in ignoreResult.IgnoredFiles)
        {
            logger.LogMatch(ignored.FilePath, ignored.MatchedPattern);
        }
    }
    logger.LogInfo($"Result: {ignoreResult.IgnoredFiles.Count} ignored, {ignoreResult.ActiveFiles.Count} active");

    var ignoredFiles = ignoreResult.IgnoredFiles.Select(f => f.FilePath).ToList();
    return (ignoreResult.ActiveFiles, ignoredFiles);
}

static TestSelectionResult HandleAllFilesIgnored(
    DiagnosticLogger logger,
    TestSelectorConfig? config,
    List<string> ignoredFiles)
{
    logger.LogDecision("Skip all tests", "All changed files are ignored");
    var result = TestSelectionResult.AllIgnored(ignoredFiles);
    InitializeCategories(result, config);
    logger.LogSummary(false, "all_ignored", 0, []);
    return result;
}

static TestSelectionResult? CheckTriggerAllPaths(
    DiagnosticLogger logger,
    TestSelectorConfig? config,
    List<string> activeFiles,
    List<string> ignoredFiles)
{
    logger.LogStep("Check TriggerAll Paths");

    var triggerAllPatterns = config?.TriggerAllPaths ?? [];
    var triggerAllDetector = new CriticalFileDetector(triggerAllPatterns);
    logger.LogInfo($"TriggerAll patterns: {triggerAllDetector.TriggerPatterns.Count}");

    var triggerAllFileInfo = triggerAllDetector.FindFirstCriticalFileWithDetails(activeFiles);

    if (triggerAllFileInfo is not null)
    {
        logger.LogWarning("TriggerAll file detected!");
        logger.LogMatch(triggerAllFileInfo.FilePath, triggerAllFileInfo.MatchedPattern);
        logger.LogDecision("Run ALL tests", "File matched triggerAllPaths");

        var result = TestSelectionResult.CriticalPath(triggerAllFileInfo.FilePath, triggerAllFileInfo.MatchedPattern);
        result.ChangedFiles = activeFiles;
        result.IgnoredFiles = ignoredFiles;
        InitializeCategories(result, config, allEnabled: true);
        logger.LogSummary(true, "trigger_all_path", 0, []);
        return result;
    }

    logger.LogSuccess("No triggerAll files detected");
    return null;
}

static async Task<TestSelectionResult> EvaluateWithDotnetAffectedOnlyAsync(
    DiagnosticLogger logger,
    List<string> activeFiles,
    List<string> ignoredFiles,
    string solution,
    string? fromRef,
    string? toRef,
    string workingDir,
    string ciEnvironment,
    bool verbose)
{
    logger.LogStep("Run dotnet-affected (no config - category logic skipped)");

    // If no fromRef, we can't run dotnet-affected (it needs git refs)
    // In this case, run all tests as a conservative fallback
    if (string.IsNullOrEmpty(fromRef))
    {
        logger.LogWarning("No --from ref provided - cannot run dotnet-affected without git refs");
        logger.LogDecision("Run ALL tests", "Conservative fallback - no git ref to compare against");

        var fallbackResult = TestSelectionResult.RunAll("No git ref provided (--from) - cannot determine affected projects");
        fallbackResult.ChangedFiles = activeFiles;
        fallbackResult.IgnoredFiles = ignoredFiles;
        logger.LogSummary(true, "no_git_ref", 0, []);
        return fallbackResult;
    }

    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    logger.LogInfo($"Solution: {solutionPath}");
    logger.LogInfo($"Comparing: {fromRef} → {toRef ?? "HEAD"}");

    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedResult = await affectedRunner.RunAsync(fromRef, toRef).ConfigureAwait(false);

    if (!affectedResult.Success)
    {
        WriteError(ciEnvironment, $"dotnet-affected failed (exit code {affectedResult.ExitCode}): {affectedResult.Error}");
        logger.LogWarning($"dotnet-affected failed (exit code {affectedResult.ExitCode})");
        logger.LogDecision("Run ALL tests", "Conservative fallback due to dotnet-affected failure");

        var errorResult = TestSelectionResult.RunAll($"dotnet-affected failed: {affectedResult.Error}");
        errorResult.ChangedFiles = activeFiles;
        errorResult.IgnoredFiles = ignoredFiles;
        logger.LogSummary(true, "dotnet_affected_failed", 0, []);
        return errorResult;
    }

    logger.LogSuccess($"dotnet-affected succeeded: {affectedResult.AffectedProjects.Count} affected projects");
    logger.LogList("Affected projects", affectedResult.AffectedProjects);

    // Filter to test projects using default patterns
    var testProjectFilter = new TestProjectFilter(new IncludeExcludePatterns());
    var filterResult = testProjectFilter.FilterWithDetails(affectedResult.AffectedProjects);
    var testProjects = filterResult.TestProjects.Select(p => p.Path).ToList();

    logger.LogInfo($"Test projects: {testProjects.Count}");

    var result = new TestSelectionResult
    {
        RunAllTests = false,
        Reason = "selective_dotnet_affected_only",
        ChangedFiles = activeFiles,
        IgnoredFiles = ignoredFiles,
        DotnetAffectedProjects = affectedResult.AffectedProjects,
        AffectedTestProjects = testProjects,
        Categories = [],
        IntegrationsProjects = testProjects
    };

    logger.LogSummary(false, "selective_dotnet_affected_only", testProjects.Count, testProjects);
    return result;
}

static (Dictionary<string, bool> CategoryStatus, List<string> MatchedFiles) MatchFilesToCategories(
    DiagnosticLogger logger,
    TestSelectorConfig config,
    List<string> activeFiles)
{
    logger.LogStep("Match Files to Categories");
    var categoryMapper = new CategoryMapper(config.Categories);
    var categoryResult = categoryMapper.GetCategoriesWithDetails(activeFiles);

    foreach (var (categoryName, matches) in categoryResult.CategoryMatches)
    {
        if (matches.Count > 0)
        {
            logger.LogSubSection($"Category '{categoryName}' triggered by:");
            foreach (var match in matches)
            {
                logger.LogMatch(match.FilePath, match.MatchedPattern);
            }
        }
    }

    logger.LogCategories("Category status", categoryResult.CategoryStatus);
    logger.LogInfo($"Files matched by categories: {categoryResult.MatchedFiles.Count}");

    return (categoryResult.CategoryStatus, categoryResult.MatchedFiles.ToList());
}

static (List<string> MappedProjects, List<string> MatchedFiles) ApplySourceToTestMappings(
    DiagnosticLogger logger,
    TestSelectorConfig config,
    List<string> activeFiles)
{
    logger.LogStep("Apply Source-to-Test Mappings");
    var projectMappingResolver = new ProjectMappingResolver(config.SourceToTestMappings);
    logger.LogInfo($"Source-to-test mappings configured: {projectMappingResolver.MappingCount}");

    var mappingResult = projectMappingResolver.ResolveAllWithDetails(activeFiles);

    if (mappingResult.Mappings.Count > 0)
    {
        logger.LogSubSection("Files matched by source-to-test mappings");
        foreach (var mapping in mappingResult.Mappings)
        {
            var detail = mapping.CapturedName is not null
                ? $"captured {{name}}={mapping.CapturedName}"
                : null;
            logger.LogMatch(mapping.SourceFile, mapping.SourcePattern, detail);
            logger.LogInfo($"      → test project: {mapping.TestProject}");
        }
    }

    logger.LogInfo($"Resolved test projects from mappings: {mappingResult.TestProjects.Count}");
    logger.LogInfo($"Files matched: {mappingResult.MatchedFiles.Count}");

    return (mappingResult.TestProjects.ToList(), mappingResult.MatchedFiles.ToList());
}

static TestSelectionResult? CheckUnmatchedFiles(
    DiagnosticLogger logger,
    TestSelectorConfig config,
    List<string> activeFiles,
    List<string> ignoredFiles,
    List<string> pathMatchedFiles,
    List<string> mappingMatchedFiles)
{
    logger.LogStep("Check for Unmatched Files");
    var allMatchedFiles = pathMatchedFiles
        .Union(mappingMatchedFiles)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var unmatchedFiles = activeFiles.Where(f => !allMatchedFiles.Contains(f.Replace('\\', '/'))).ToList();

    logger.LogInfo($"Total files: {activeFiles.Count}");
    logger.LogInfo($"Matched by categories: {pathMatchedFiles.Count}");
    logger.LogInfo($"Matched by source-to-test mappings: {mappingMatchedFiles.Count}");
    logger.LogInfo($"Unmatched files: {unmatchedFiles.Count}");

    if (unmatchedFiles.Count > 0)
    {
        logger.LogWarning("Unmatched files found - triggering conservative fallback");
        logger.LogList("Unmatched files", unmatchedFiles);
        logger.LogDecision("Run ALL tests", "Conservative fallback due to unmatched files");

        var reason = unmatchedFiles.Count <= 5
            ? $"Unmatched files: {string.Join(", ", unmatchedFiles)}"
            : $"Unmatched files ({unmatchedFiles.Count}): {string.Join(", ", unmatchedFiles.Take(5))}...";

        var unmatchedResult = TestSelectionResult.RunAll(reason);
        unmatchedResult.ChangedFiles = activeFiles;
        unmatchedResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(unmatchedResult, config, allEnabled: true);
        logger.LogSummary(true, reason, 0, []);
        return unmatchedResult;
    }

    logger.LogSuccess("All files are accounted for");
    return null;
}

static async Task<(List<string> AffectedProjects, TestSelectionResult? FallbackResult)> RunDotnetAffectedAsync(
    DiagnosticLogger logger,
    TestSelectorConfig config,
    List<string> activeFiles,
    List<string> ignoredFiles,
    string solution,
    string? fromRef,
    string? toRef,
    string workingDir,
    string ciEnvironment,
    bool verbose)
{
    logger.LogStep("Run dotnet-affected");

    // If no fromRef, we can't run dotnet-affected (it needs git refs)
    if (string.IsNullOrEmpty(fromRef))
    {
        logger.LogWarning("No --from ref provided - cannot run dotnet-affected without git refs");
        logger.LogDecision("Run ALL tests", "Conservative fallback - no git ref to compare against");

        var fallbackResult = TestSelectionResult.RunAll("No git ref provided (--from) - cannot determine affected projects");
        fallbackResult.ChangedFiles = activeFiles;
        fallbackResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(fallbackResult, config, allEnabled: true);
        logger.LogSummary(true, "no_git_ref", 0, []);
        return ([], fallbackResult);
    }

    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    logger.LogInfo($"Solution: {solutionPath}");
    logger.LogInfo($"Comparing: {fromRef} → {toRef ?? "HEAD"}");

    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedResult = await affectedRunner.RunAsync(fromRef, toRef).ConfigureAwait(false);

    if (affectedResult.Success)
    {
        logger.LogSuccess($"dotnet-affected succeeded: {affectedResult.AffectedProjects.Count} affected projects");
        logger.LogList("Affected projects", affectedResult.AffectedProjects);
        return (affectedResult.AffectedProjects, null);
    }

    // dotnet-affected failed - conservative fallback
    WriteError(ciEnvironment, $"dotnet-affected failed (exit code {affectedResult.ExitCode}): {affectedResult.Error}");
    logger.LogWarning($"dotnet-affected failed (exit code {affectedResult.ExitCode})");
    if (!string.IsNullOrWhiteSpace(affectedResult.Error))
    {
        logger.LogInfo($"Error: {affectedResult.Error}");
    }
    if (!string.IsNullOrWhiteSpace(affectedResult.StdOut))
    {
        logger.LogInfo($"stdout: {affectedResult.StdOut.Trim()}");
    }
    if (!string.IsNullOrWhiteSpace(affectedResult.StdErr))
    {
        logger.LogInfo($"stderr: {affectedResult.StdErr.Trim()}");
    }

    logger.LogDecision("Run ALL tests", "Conservative fallback due to dotnet-affected failure");

    var errorResult = TestSelectionResult.RunAll($"dotnet-affected failed: {affectedResult.Error}");
    errorResult.ChangedFiles = activeFiles;
    errorResult.IgnoredFiles = ignoredFiles;
    InitializeCategories(errorResult, config, allEnabled: true);
    logger.LogSummary(true, "dotnet_affected_failed", 0, []);
    return ([], errorResult);
}

static List<string> FilterAndCombineTestProjects(
    DiagnosticLogger logger,
    TestSelectorConfig config,
    List<string> affectedProjects,
    List<string> mappedProjects)
{
    logger.LogStep("Filter Test Projects");
    var testProjectFilter = new TestProjectFilter(config.TestProjectPatterns);
    logger.LogInfo($"Include patterns: {string.Join(", ", testProjectFilter.IncludePatterns)}");
    logger.LogInfo($"Exclude patterns: {string.Join(", ", testProjectFilter.ExcludePatterns)}");

    var filterResult = testProjectFilter.FilterWithDetails(affectedProjects);

    if (filterResult.TestProjects.Count > 0)
    {
        logger.LogSubSection("Test projects (from dotnet-affected)");
        foreach (var proj in filterResult.TestProjects)
        {
            logger.LogInfo($"    • {proj.Name ?? proj.Path}");
        }
    }

    var testProjects = filterResult.TestProjects.Select(p => p.Path).ToList();
    logger.LogInfo($"Test projects from dotnet-affected: {testProjects.Count}");

    logger.LogStep("Combine Test Projects");
    var allTestProjects = testProjects
        .Concat(mappedProjects)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    logger.LogInfo($"From dotnet-affected: {testProjects.Count}");
    logger.LogInfo($"From source-to-test mappings: {mappedProjects.Count}");
    logger.LogInfo($"Total unique test projects: {allTestProjects.Count}");

    return allTestProjects;
}

static void UpdateCategoriesFromTestProjects(
    DiagnosticLogger logger,
    TestSelectorConfig config,
    Dictionary<string, bool> pathTriggeredCategories,
    List<string> allTestProjects)
{
    logger.LogStep("Match Test Projects to Categories");
    var categoryMapper = new CategoryMapper(config.Categories);
    var testProjectCategoryResult = categoryMapper.GetCategoriesWithDetails(allTestProjects);

    foreach (var (categoryName, enabled) in testProjectCategoryResult.CategoryStatus)
    {
        if (enabled && !pathTriggeredCategories.GetValueOrDefault(categoryName))
        {
            logger.LogInfo($"Category '{categoryName}' additionally triggered by test project paths");
            pathTriggeredCategories[categoryName] = true;
        }
    }
}

static TestSelectionResult BuildSelectiveResult(
    DiagnosticLogger logger,
    List<string> activeFiles,
    List<string> ignoredFiles,
    List<string> affectedProjects,
    List<string> allTestProjects,
    Dictionary<string, bool> categories)
{
    logger.LogStep("Build Final Result");
    var finalResult = new TestSelectionResult
    {
        RunAllTests = false,
        Reason = "selective",
        ChangedFiles = activeFiles,
        IgnoredFiles = ignoredFiles,
        DotnetAffectedProjects = affectedProjects,
        AffectedTestProjects = allTestProjects,
        Categories = categories,
        IntegrationsProjects = allTestProjects
    };

    logger.LogSummary(false, "selective", allTestProjects.Count, allTestProjects);

    return finalResult;
}

static void InitializeCategories(TestSelectionResult result, TestSelectorConfig? config, bool allEnabled = false)
{
    result.Categories = [];
    if (config is not null)
    {
        foreach (var categoryName in config.Categories.Keys)
        {
            result.Categories[categoryName] = allEnabled;
        }
    }
}

// Detects the CI environment based on environment variables.
// Returns "GitHub", "AzureDevOps", or "Local".
static string DetectCIEnvironment()
{
    // GitHub Actions sets GITHUB_ACTIONS=true
    if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
    {
        return "GitHub";
    }

    // Azure DevOps sets TF_BUILD=True
    if (Environment.GetEnvironmentVariable("TF_BUILD") == "True")
    {
        return "AzureDevOps";
    }

    return "Local";
}

// Writes an error message in a format appropriate for the CI environment.
static void WriteError(string environment, string message)
{
    var formatted = environment switch
    {
        "GitHub" => $"::error::{message}",
        "AzureDevOps" => $"##vso[task.logissue type=error]{message}",
        _ => $"Error: {message}"
    };
    Console.Error.WriteLine(formatted);
}
