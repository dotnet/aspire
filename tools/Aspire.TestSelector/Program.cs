// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.TestSelector;
using Aspire.TestSelector.Analyzers;
using Aspire.TestSelector.Models;

// IMPORTANT: MSBuild initialization must happen before any MSBuild types are loaded.
// This must be the first statement that runs, before any code that references Microsoft.Build.
MSBuildProjectEvaluator.Initialize();

// Define CLI options
var solutionOption = new Option<string>("--solution", "-s") { Description = "Path to the solution file (.sln or .slnx)" }; // FIXME: should be required
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
            // FIXME: do we need this? CI yml could pass this in
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
        // FIXME: this should be inline - small method and used once
        return HandleNoChanges(config, logger);
    }

    logger.LogInfo($"Processing {changedFiles.Count} changed files");

    // Step 1: Filter ignored files
    var (ignoredFiles, activeFiles) = FilterIgnoredFiles(config, changedFiles, logger);

    // All files ignored
    if (activeFiles.Count == 0)
    {
        // FIXME: this should be inline
        return HandleAllFilesIgnored(config, ignoredFiles, logger);
    }

    // Step 2: Check for triggerAll categories
    var triggerAllResult = CheckTriggerAll(config, activeFiles, ignoredFiles, logger);
    if (triggerAllResult != null)
    {
        return triggerAllResult;
    }

    // Step 3: Match files to categories via triggerPaths
    var (pathTriggeredCategories, pathMatchedFiles) = MatchFilesToCategories(config, activeFiles, logger);

    // Step 4: Run dotnet-affected
    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    var (affectedProjects, dotnetMatchedFiles, dotnetAffectedFailed) = await RunDotnetAffectedAsync(
        solutionPath, workingDir, fromRef, toRef, activeFiles, verbose, logger).ConfigureAwait(false);

    if (dotnetAffectedFailed != null)
    {
        return HandleDotnetAffectedFailure(config, activeFiles, ignoredFiles, dotnetAffectedFailed, logger);
    }

    // Step 5: Apply projectMappings
    var (mappedProjects, mappingMatchedFiles) = ApplyProjectMappings(config, activeFiles, logger);

    // Step 6: Check for unmatched files
    var unmatchedFilesResult = CheckUnmatchedFiles(
        config, activeFiles, ignoredFiles, pathMatchedFiles, dotnetMatchedFiles, mappingMatchedFiles, logger);
    if (unmatchedFilesResult != null)
    {
        return unmatchedFilesResult;
    }

    // Step 7: Classify affected projects
    using var msbuildEvaluator = new MSBuildProjectEvaluator(workingDir);
    var (testProjects, sourceProjects, projectFilter) = SplitAffectedSourceAndTestProjects(affectedProjects, workingDir, msbuildEvaluator, logger);

    // Step 8: Check NuGet-dependent tests
    var nugetInfo = CheckNuGetDependentTests(sourceProjects, projectFilter, msbuildEvaluator, logger);

    // Step 9: Combine test projects
    var allTestProjects = CombineTestProjects(testProjects, mappedProjects, nugetInfo, logger);

    // Step 10: Build final result
    return BuildFinalResult(
        activeFiles, ignoredFiles, affectedProjects, allTestProjects, pathTriggeredCategories, nugetInfo, logger);
}

static TestSelectionResult HandleNoChanges(TestSelectorConfig config, DiagnosticLogger logger)
{
    logger.LogInfo("No changed files detected");
    var result = TestSelectionResult.NoChanges();
    InitializeCategories(result, config);
    logger.LogSummary(false, "no_changes", 0, []);
    return result;
}

static (List<string> IgnoredFiles, List<string> ActiveFiles) FilterIgnoredFiles(
    TestSelectorConfig config,
    List<string> changedFiles,
    DiagnosticLogger logger)
{
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
    return (ignoredFiles, ignoreResult.ActiveFiles);
}

static TestSelectionResult HandleAllFilesIgnored(
    TestSelectorConfig config,
    List<string> ignoredFiles,
    DiagnosticLogger logger)
{
    logger.LogDecision("Skip all tests", "All changed files are ignored");
    var result = TestSelectionResult.AllIgnored(ignoredFiles);
    InitializeCategories(result, config);
    logger.LogSummary(false, "all_ignored", 0, []);
    return result;
}

static TestSelectionResult? CheckTriggerAll(
    TestSelectorConfig config,
    List<string> activeFiles,
    List<string> ignoredFiles,
    DiagnosticLogger logger)
{
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

        var result = TestSelectionResult.CriticalPath(criticalFileInfo.FilePath, criticalFileInfo.MatchedPattern);
        result.ChangedFiles = activeFiles;
        result.IgnoredFiles = ignoredFiles;
        InitializeCategories(result, config, allEnabled: true);
        logger.LogSummary(true, "critical_path", 0, []);
        return result;
    }

    logger.LogSuccess("No critical files detected");
    return null;
}

static (Dictionary<string, bool> Categories, HashSet<string> MatchedFiles) MatchFilesToCategories(
    TestSelectorConfig config,
    List<string> activeFiles,
    DiagnosticLogger logger)
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

    return (categoryResult.CategoryStatus, categoryResult.MatchedFiles);
}

static async Task<(List<string> AffectedProjects, HashSet<string> MatchedFiles, string? FailureReason)> RunDotnetAffectedAsync(
    string solutionPath,
    string workingDir,
    string fromRef,
    string? toRef,
    List<string> activeFiles,
    bool verbose,
    DiagnosticLogger logger)
{
    logger.LogStep("Run dotnet-affected");
    logger.LogInfo($"Solution: {solutionPath}");
    logger.LogInfo($"Comparing: {fromRef} → {toRef ?? "HEAD"}");

    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedResult = await affectedRunner.RunAsync(fromRef, toRef).ConfigureAwait(false);

    if (affectedResult.Success)
    {
        var dotnetMatchedFiles = GetFilesInSolution(activeFiles, solutionPath, workingDir);

        logger.LogSuccess($"dotnet-affected succeeded: {affectedResult.AffectedProjects.Count} affected projects");
        logger.LogList("Affected projects", affectedResult.AffectedProjects);
        logger.LogInfo($"Files in solution scope: {dotnetMatchedFiles.Count}");

        return (affectedResult.AffectedProjects, dotnetMatchedFiles, null);
    }

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

    return ([], [], affectedResult.Error ?? "Unknown error");
}

static TestSelectionResult HandleDotnetAffectedFailure(
    TestSelectorConfig config,
    List<string> activeFiles,
    List<string> ignoredFiles,
    string errorMessage,
    DiagnosticLogger logger)
{
    logger.LogDecision("Run ALL tests", "Conservative fallback due to dotnet-affected failure");

    var result = TestSelectionResult.RunAll($"dotnet-affected failed: {errorMessage}");
    result.ChangedFiles = activeFiles;
    result.IgnoredFiles = ignoredFiles;
    InitializeCategories(result, config, allEnabled: true);
    logger.LogSummary(true, "dotnet_affected_failed", 0, []);
    return result;
}

static (List<string> MappedProjects, HashSet<string> MatchedFiles) ApplyProjectMappings(
    TestSelectorConfig config,
    List<string> activeFiles,
    DiagnosticLogger logger)
{
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

    logger.LogInfo($"Resolved test projects: {mappingResult.TestProjects.Count}");
    logger.LogInfo($"Files matched: {mappingResult.MatchedFiles.Count}");

    return (mappingResult.TestProjects.ToList(), mappingResult.MatchedFiles);
}

static TestSelectionResult? CheckUnmatchedFiles(
    TestSelectorConfig config,
    List<string> activeFiles,
    List<string> ignoredFiles,
    HashSet<string> pathMatchedFiles,
    HashSet<string> dotnetMatchedFiles,
    HashSet<string> mappingMatchedFiles,
    DiagnosticLogger logger)
{
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

        var reason = unmatchedFiles.Count <= 5
            ? $"Unmatched files: {string.Join(", ", unmatchedFiles)}"
            : $"Unmatched files ({unmatchedFiles.Count}): {string.Join(", ", unmatchedFiles.Take(5))}...";

        var result = TestSelectionResult.RunAll(reason);
        result.ChangedFiles = activeFiles;
        result.IgnoredFiles = ignoredFiles;
        InitializeCategories(result, config, allEnabled: true);
        logger.LogSummary(true, reason, 0, []);
        return result;
    }

    logger.LogSuccess("All files are accounted for");
    return null;
}

static (List<string> TestProjects, List<string> SourceProjects, TestProjectFilter Filter) SplitAffectedSourceAndTestProjects(
    List<string> affectedProjects,
    string workingDir,
    MSBuildProjectEvaluator msbuildEvaluator,
    DiagnosticLogger logger)
{
    logger.LogStep("Classify Affected Projects");
    var projectFilter = new TestProjectFilter(workingDir, msbuildEvaluator);
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

    return (testProjects, sourceProjects, projectFilter);
}

static NuGetDependentTestsInfo CheckNuGetDependentTests(
    List<string> sourceProjects,
    TestProjectFilter projectFilter,
    MSBuildProjectEvaluator msbuildEvaluator,
    DiagnosticLogger logger)
{
    logger.LogStep("Check NuGet-Dependent Tests");
    var nugetChecker = NuGetDependencyChecker.Create(projectFilter, msbuildEvaluator);
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

    return nugetInfo;
}

static List<string> CombineTestProjects(
    List<string> testProjects,
    List<string> mappedProjects,
    NuGetDependentTestsInfo nugetInfo,
    DiagnosticLogger logger)
{
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

    return allTestProjects;
}

static TestSelectionResult BuildFinalResult(
    List<string> activeFiles,
    List<string> ignoredFiles,
    List<string> affectedProjects,
    List<string> allTestProjects,
    Dictionary<string, bool> pathTriggeredCategories,
    NuGetDependentTestsInfo nugetInfo,
    DiagnosticLogger logger)
{
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
