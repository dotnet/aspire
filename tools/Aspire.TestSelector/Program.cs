// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.TestSelector;
using Aspire.TestSelector.Analyzers;
using Aspire.TestSelector.Models;

// Define CLI options
// FIXME: make this a required option
var solutionOption = new Option<string>("--solution", "-s") { Description = "Path to the solution file (.sln or .slnx)" };
var configOption = new Option<string>("--config", "-c") { Description = "Path to the test selector configuration file" };
var fromOption = new Option<string>("--from", "-f") { Description = "Git ref to compare from (e.g., origin/main)" };
var toOption = new Option<string?>("--to", "-t") { Description = "Git ref to compare to (default: HEAD)" };
var changedFilesOption = new Option<string?>("--changed-files") { Description = "Comma-separated list of changed files (for testing without git)" };
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
    // FIXME: remove the default value once we have a required option
    var solution = result.GetValue(solutionOption) ?? "Aspire.slnx";
    // FIXME: this should be allowed to be null, and the code should still work correctly
    var configPath = result.GetValue(configOption) ?? "eng/scripts/test-selection-rules.json";
    var fromRef = result.GetValue(fromOption) ?? "origin/main";
    var toRef = result.GetValue(toOption);
    // FIXME: if changedFiled is provided then we should not use git at all
    var changedFilesStr = result.GetValue(changedFilesOption);
    var outputPath = result.GetValue(outputOption);
    var githubOutput = result.GetValue(githubOutputOption);
    var verbose = result.GetValue(verboseOption);

    var workingDir = Directory.GetCurrentDirectory();

    if (verbose)
    {
        Console.WriteLine($"Working directory: {workingDir}");
        Console.WriteLine($"Solution: {solution}");
        Console.WriteLine($"Config: {configPath}");
        Console.WriteLine($"From ref: {fromRef}");
        Console.WriteLine($"To ref: {toRef ?? "HEAD"}");
    }

    try
    {
        // Load configuration
        var configFullPath = Path.IsPathRooted(configPath) ? configPath : Path.Combine(workingDir, configPath);
        if (!File.Exists(configFullPath))
        {
            Console.Error.WriteLine($"Error: Configuration file not found: {configFullPath}");
            Environment.Exit(1);
            return;
        }

        var config = TestSelectorConfig.LoadFromFile(configFullPath);
        if (verbose)
        {
            Console.WriteLine($"Loaded config with {config.IgnorePaths.Count} ignore patterns");
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
            changedFiles = GetGitChangedFiles(fromRef, toRef, workingDir);

            if (verbose)
            {
                Console.WriteLine($"Found {changedFiles.Count} changed files from git diff");
            }
        }

        // Run the evaluation
        var evaluationResult = await EvaluateAsync(config, changedFiles, solution, fromRef, toRef, workingDir, verbose).ConfigureAwait(false);

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
        // FIXME: support working with github or azdo. detect the environment
        Console.Error.WriteLine($"::error::Test selector failed: {ex.Message}");
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

static async Task<TestSelectionResult> EvaluateAsync(
    TestSelectorConfig config,
    List<string> changedFiles,
    string solution,
    string fromRef,
    string? toRef,
    string workingDir,
    bool verbose)
{
    var logger = new DiagnosticLogger(verbose);

    // No changes
    if (changedFiles.Count == 0)
    {
        logger.LogInfo("No changed files detected");
        var result = TestSelectionResult.NoChanges();
        InitializeCategories(result, config);
        logger.LogSummary(false, "no_changes", 0, []);
        return result;
    }

    logger.LogInfo($"Processing {changedFiles.Count} changed files");

    // FIXME: move the various steps into separate methods, so we have a simple readable flow here
    // FIXME: add ascii diagram of the flow in the comments
    // Step 1: Filter ignored files
    logger.LogStep("Filter Ignored Files");
    var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
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
    var activeFiles = ignoreResult.ActiveFiles;

    // All files ignored
    if (activeFiles.Count == 0)
    {
        logger.LogDecision("Skip all tests", "All changed files are ignored");
        var result = TestSelectionResult.AllIgnored(ignoredFiles);
        InitializeCategories(result, config);
        logger.LogSummary(false, "all_ignored", 0, []);
        return result;
    }

    // Step 2: Check for critical files (triggerAllPaths)
    logger.LogStep("Check Critical Files (triggerAllPaths)");
    var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths);
    logger.LogInfo($"Critical patterns: {criticalDetector.TriggerPatterns.Count}");

    // FIXME: instead of "critical" we should call these triggerAll everywhere
    var criticalFileInfo = criticalDetector.FindFirstCriticalFileWithDetails(activeFiles);

    if (criticalFileInfo is not null)
    {
        logger.LogWarning("Critical file detected!");
        logger.LogMatch(criticalFileInfo.FilePath, criticalFileInfo.MatchedPattern);
        logger.LogDecision("Run ALL tests", "Critical file matched triggerAllPaths");

        var result = TestSelectionResult.CriticalPath(criticalFileInfo.FilePath, criticalFileInfo.MatchedPattern);
        result.ChangedFiles = activeFiles;
        result.IgnoredFiles = ignoredFiles;
        InitializeCategories(result, config, allEnabled: true);
        logger.LogSummary(true, "critical_path", 0, []);
        return result;
    }

    logger.LogSuccess("No critical files detected");

    // Step 3: Match files to categories via triggerPaths
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

    var pathTriggeredCategories = categoryResult.CategoryStatus;
    var pathMatchedFiles = categoryResult.MatchedFiles;

    // Step 4: Apply sourceToTestMappings to expand changed files with test project hints
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

    var mappedProjects = mappingResult.TestProjects.ToList();
    var mappingMatchedFiles = mappingResult.MatchedFiles;

    // Step 5: Run dotnet-affected for transitive dependency analysis
    logger.LogStep("Run dotnet-affected");
    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    logger.LogInfo($"Solution: {solutionPath}");
    logger.LogInfo($"Comparing: {fromRef} → {toRef ?? "HEAD"}");

    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedResult = await affectedRunner.RunAsync(fromRef, toRef).ConfigureAwait(false);

    List<string> affectedProjects;

    if (affectedResult.Success)
    {
        logger.LogSuccess($"dotnet-affected succeeded: {affectedResult.AffectedProjects.Count} affected projects");
        logger.LogList("Affected projects", affectedResult.AffectedProjects);

        affectedProjects = affectedResult.AffectedProjects;
    }
    else
    {
        // dotnet-affected failed - conservative fallback
        Console.Error.WriteLine($"::error::dotnet-affected failed (exit code {affectedResult.ExitCode}): {affectedResult.Error}");
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
        return errorResult;
    }

    // Step 6: Check for unmatched files
    // Files must be explicitly matched by categories or sourceToTestMappings.
    // This is conservative: any unmatched file triggers all tests.
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

    // Step 7: Filter affected projects to get test projects only
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

    // Step 8: Combine test projects from all sources
    logger.LogStep("Combine Test Projects");
    var allTestProjects = testProjects
        .Concat(mappedProjects)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    logger.LogInfo($"From dotnet-affected: {testProjects.Count}");
    logger.LogInfo($"From source-to-test mappings: {mappedProjects.Count}");
    logger.LogInfo($"Total unique test projects: {allTestProjects.Count}");

    // Step 9: Match test projects against categories (categories match both source paths and test paths)
    logger.LogStep("Match Test Projects to Categories");
    var testProjectCategoryResult = categoryMapper.GetCategoriesWithDetails(allTestProjects);

    foreach (var (categoryName, enabled) in testProjectCategoryResult.CategoryStatus)
    {
        if (enabled && !pathTriggeredCategories.GetValueOrDefault(categoryName))
        {
            logger.LogInfo($"Category '{categoryName}' additionally triggered by test project paths");
            pathTriggeredCategories[categoryName] = true;
        }
    }

    // Step 10: Build final result
    logger.LogStep("Build Final Result");
    var finalResult = new TestSelectionResult
    {
        RunAllTests = false,
        Reason = "selective",
        ChangedFiles = activeFiles,
        IgnoredFiles = ignoredFiles,
        DotnetAffectedProjects = affectedProjects,
        AffectedTestProjects = allTestProjects,
        Categories = pathTriggeredCategories,
        IntegrationsProjects = allTestProjects
    };

    logger.LogSummary(false, "selective", allTestProjects.Count, allTestProjects);

    return finalResult;
}

static void InitializeCategories(TestSelectionResult result, TestSelectorConfig config, bool allEnabled = false)
{
    result.Categories = [];
    foreach (var categoryName in config.Categories.Keys)
    {
        result.Categories[categoryName] = allEnabled;
    }
}
