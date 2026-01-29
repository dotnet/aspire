// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.TestSelector;
using Aspire.TestSelector.Analyzers;
using Aspire.TestSelector.Models;

// Define CLI options
var solutionOption = new Option<string>("--solution", "-s") { Description = "Path to the solution file (.sln or .slnx)" };
var configOption = new Option<string>("--config", "-c") { Description = "Path to the test selector configuration file" };
var fromOption = new Option<string>("--from", "-f") { Description = "Git ref to compare from (e.g., origin/main)" };
var toOption = new Option<string?>("--to", "-t") { Description = "Git ref to compare to (default: HEAD)" };
var changedFilesOption = new Option<string?>("--changed-files") { Description = "Comma-separated list of changed files (for testing without git)" };
var outputOption = new Option<string?>("--output", "-o") { Description = "Output file path for the JSON result" };
var githubOutputOption = new Option<bool>("--github-output") { Description = "Output in GitHub Actions format" };
var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };

var rootCommand = new RootCommand("MSBuild-based test selection tool for Aspire")
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
    var solution = result.GetValue(solutionOption) ?? "Aspire.slnx";
    var configPath = result.GetValue(configOption) ?? "eng/scripts/test-selection-rules.json";
    var fromRef = result.GetValue(fromOption) ?? "origin/main";
    var toRef = result.GetValue(toOption);
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
    if (process == null)
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
        var noChangesResult = TestSelectionResult.NoChanges();
        InitializeCategories(noChangesResult, config);
        logger.LogSummary(false, "no_changes", 0, []);
        return noChangesResult;
    }

    logger.LogInfo($"Processing {changedFiles.Count} changed files");

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
        var ignoredResult = TestSelectionResult.AllIgnored(ignoredFiles);
        InitializeCategories(ignoredResult, config);
        logger.LogSummary(false, "all_ignored", 0, []);
        return ignoredResult;
    }

    // Step 2: Check for critical files (triggerAll categories)
    logger.LogStep("Check Critical Files (triggerAll)");
    var criticalDetector = CriticalFileDetector.FromCategories(config.Categories);
    var triggerAllCategories = criticalDetector.GetTriggerAllCategories().ToList();
    logger.LogInfo($"Categories with triggerAll=true: {string.Join(", ", triggerAllCategories)}");
    logger.LogInfo($"Critical patterns: {criticalDetector.TriggerPatterns.Count}");

    var criticalFileInfo = criticalDetector.FindFirstCriticalFileWithDetails(activeFiles);

    if (criticalFileInfo != null)
    {
        logger.LogWarning("Critical file detected!");
        logger.LogMatch(criticalFileInfo.FilePath, criticalFileInfo.MatchedPattern, $"category: {criticalFileInfo.Category ?? "unknown"}");
        logger.LogDecision("Run ALL tests", $"Critical file matched triggerAll pattern");

        var criticalResult = TestSelectionResult.CriticalPath(criticalFileInfo.FilePath, criticalFileInfo.MatchedPattern);
        criticalResult.ChangedFiles = activeFiles;
        criticalResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(criticalResult, config, allEnabled: true);
        logger.LogSummary(true, "critical_path", 0, []);
        return criticalResult;
    }

    logger.LogSuccess("No critical files detected");

    // Step 3a: Match files to categories via triggerPaths
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

    var pathTriggeredCategories = categoryResult.CategoryStatus;
    var pathMatchedFiles = categoryResult.MatchedFiles;
    logger.LogCategories("Category status", pathTriggeredCategories);
    logger.LogInfo($"Files matched by categories: {pathMatchedFiles.Count}");

    // Step 3b: Run dotnet-affected
    logger.LogStep("Run dotnet-affected");
    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    logger.LogInfo($"Solution: {solutionPath}");
    logger.LogInfo($"Comparing: {fromRef} → {toRef ?? "HEAD"}");

    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedResult = await affectedRunner.RunAsync(fromRef, toRef).ConfigureAwait(false);

    List<string> affectedProjects = [];
    HashSet<string> dotnetMatchedFiles = [];

    if (affectedResult.Success)
    {
        affectedProjects = affectedResult.AffectedProjects;
        // Consider all files in the solution as "matched" by dotnet-affected
        // This is an approximation - ideally we'd track which files led to which affected projects
        dotnetMatchedFiles = GetFilesInSolution(activeFiles, solutionPath, workingDir);

        logger.LogSuccess($"dotnet-affected succeeded: {affectedProjects.Count} affected projects");
        logger.LogList("Affected projects", affectedProjects);
        logger.LogInfo($"Files in solution scope: {dotnetMatchedFiles.Count}");
    }
    else
    {
        // Always show the error - this is important for debugging CI failures
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

        // CONSERVATIVE FALLBACK: dotnet-affected failed, run ALL tests
        var fallbackResult = TestSelectionResult.RunAll($"dotnet-affected failed: {affectedResult.Error}");
        fallbackResult.ChangedFiles = activeFiles;
        fallbackResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(fallbackResult, config, allEnabled: true);
        logger.LogSummary(true, "dotnet_affected_failed", 0, []);
        return fallbackResult;
    }

    // Step 3c: Apply projectMappings
    logger.LogStep("Apply Project Mappings");
    var projectMappingResolver = new ProjectMappingResolver(config.ProjectMappings);
    logger.LogInfo($"Project mappings configured: {projectMappingResolver.MappingCount}");

    var mappingResult = projectMappingResolver.ResolveAllWithDetails(activeFiles);

    if (mappingResult.Mappings.Count > 0)
    {
        logger.LogSubSection("Files matched by project mappings");
        foreach (var mapping in mappingResult.Mappings)
        {
            var detail = mapping.CapturedName != null
                ? $"captured {{name}}={mapping.CapturedName}"
                : null;
            logger.LogMatch(mapping.SourceFile, mapping.SourcePattern, detail);
            logger.LogInfo($"      → test project: {mapping.TestProject}");
        }
    }

    var mappedProjects = mappingResult.TestProjects.ToList();
    var mappingMatchedFiles = mappingResult.MatchedFiles;
    logger.LogInfo($"Resolved test projects: {mappedProjects.Count}");
    logger.LogInfo($"Files matched: {mappingMatchedFiles.Count}");

    // Step 4: Conservative fallback - check for unmatched files
    logger.LogStep("Check for Unmatched Files");
    var allMatchedFiles = pathMatchedFiles
        .Union(dotnetMatchedFiles)
        .Union(mappingMatchedFiles)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var unmatchedFiles = activeFiles.Where(f => !allMatchedFiles.Contains(f.Replace('\\', '/'))).ToList();

    logger.LogInfo($"Total files: {activeFiles.Count}");
    logger.LogInfo($"Matched by categories: {pathMatchedFiles.Count}");
    logger.LogInfo($"Matched by solution (dotnet-affected): {dotnetMatchedFiles.Count}");
    logger.LogInfo($"Matched by project mappings: {mappingMatchedFiles.Count}");
    logger.LogInfo($"Unmatched files: {unmatchedFiles.Count}");

    if (unmatchedFiles.Count > 0)
    {
        logger.LogWarning("Unmatched files found - triggering conservative fallback");
        logger.LogList("Unmatched files", unmatchedFiles);
        logger.LogDecision("Run ALL tests", "Conservative fallback due to unmatched files");

        // CONSERVATIVE FALLBACK: unmatched files, run ALL tests
        var reason = unmatchedFiles.Count <= 5
            ? $"Unmatched files: {string.Join(", ", unmatchedFiles)}"
            : $"Unmatched files ({unmatchedFiles.Count}): {string.Join(", ", unmatchedFiles.Take(5))}...";

        var fallbackResult = TestSelectionResult.RunAll(reason);
        fallbackResult.ChangedFiles = activeFiles;
        fallbackResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(fallbackResult, config, allEnabled: true);
        logger.LogSummary(true, reason, 0, []);
        return fallbackResult;
    }

    logger.LogSuccess("All files are accounted for");

    // Step 5: Filter to test projects from dotnet-affected
    logger.LogStep("Classify Affected Projects");
    var projectFilter = new TestProjectFilter(workingDir);
    var splitResult = projectFilter.SplitProjectsWithDetails(affectedProjects);

    if (splitResult.TestProjects.Count > 0)
    {
        logger.LogSubSection("Test projects (from dotnet-affected)");
        foreach (var proj in splitResult.TestProjects)
        {
            logger.LogInfo($"    • {proj.Name ?? proj.Path}");
            logger.LogInfo($"      Reason: {proj.ClassificationReason}");
        }
    }

    if (splitResult.SourceProjects.Count > 0)
    {
        logger.LogSubSection("Source projects (from dotnet-affected)");
        foreach (var proj in splitResult.SourceProjects)
        {
            logger.LogInfo($"    • {proj.Name ?? proj.Path}");
            logger.LogInfo($"      Reason: {proj.ClassificationReason}");
        }
    }

    var testProjects = splitResult.TestProjects.Select(p => p.Path).ToList();
    var sourceProjects = splitResult.SourceProjects.Select(p => p.Path).ToList();
    logger.LogInfo($"Test projects: {testProjects.Count}, Source projects: {sourceProjects.Count}");

    // Step 6: Check NuGet-dependent tests
    logger.LogStep("Check NuGet-Dependent Tests");
    var nugetChecker = NuGetDependencyChecker.CreateDefault(projectFilter);
    logger.LogInfo($"NuGet-dependent test projects: {string.Join(", ", nugetChecker.NuGetDependentTestProjects.Select(Path.GetFileNameWithoutExtension))}");

    var nugetCheckResult = nugetChecker.CheckWithDetails(sourceProjects);

    if (nugetCheckResult.PackableProjects.Count > 0)
    {
        logger.LogSubSection("Packable projects affecting NuGet tests");
        foreach (var proj in nugetCheckResult.PackableProjects)
        {
            logger.LogInfo($"    • {proj.Name ?? proj.Path}");
            logger.LogInfo($"      IsPackable reason: {proj.ClassificationReason}");
        }
    }

    var nugetInfo = nugetChecker.Check(sourceProjects);

    if (nugetInfo.Triggered)
    {
        logger.LogWarning($"NuGet-dependent tests triggered: {nugetInfo.Reason}");
        logger.LogList("Additional test projects to run", nugetInfo.Projects);
    }
    else
    {
        logger.LogSuccess("No NuGet-dependent tests triggered");
    }

    // Step 7: Combine test projects from dotnet-affected + projectMappings + NuGet checker
    logger.LogStep("Combine Test Projects");
    var allTestProjects = testProjects
        .Concat(mappedProjects)
        .Concat(nugetInfo.Triggered ? nugetInfo.Projects : [])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    logger.LogInfo($"From dotnet-affected: {testProjects.Count}");
    logger.LogInfo($"From project mappings: {mappedProjects.Count}");
    logger.LogInfo($"From NuGet dependencies: {(nugetInfo.Triggered ? nugetInfo.Projects.Count : 0)}");
    logger.LogInfo($"Total unique test projects: {allTestProjects.Count}");

    // Step 8: Build the result with category flags
    logger.LogStep("Build Final Result");
    var result = new TestSelectionResult
    {
        RunAllTests = false,
        Reason = "msbuild_analysis",
        ChangedFiles = activeFiles,
        IgnoredFiles = ignoredFiles,
        DotnetAffectedProjects = affectedProjects,
        AffectedTestProjects = allTestProjects,
        Categories = pathTriggeredCategories,
        NuGetDependentTests = nugetInfo.Triggered ? nugetInfo : null
    };

    // Set integrations projects for matrix builds
    result.IntegrationsProjects = allTestProjects;

    logger.LogSummary(false, "msbuild_analysis", allTestProjects.Count, allTestProjects);

    return result;
}

static HashSet<string> GetFilesInSolution(List<string> files, string solutionPath, string workingDir)
{
    // Consider files matched if they are in src/ or tests/ directories
    // This is a heuristic - files in these directories are likely in the solution
    var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in files)
    {
        var normalizedFile = file.Replace('\\', '/');
        if (normalizedFile.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
            normalizedFile.StartsWith("tests/", StringComparison.OrdinalIgnoreCase))
        {
            matched.Add(normalizedFile);
        }
    }

    return matched;
}

static void InitializeCategories(TestSelectionResult result, TestSelectorConfig config, bool allEnabled = false)
{
    result.Categories = [];
    foreach (var categoryName in config.Categories.Keys)
    {
        result.Categories[categoryName] = allEnabled;
    }
}
