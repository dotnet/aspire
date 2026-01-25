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

rootCommand.SetAction(result =>
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
        var evaluationResult = Evaluate(config, changedFiles, solution, fromRef, toRef, workingDir, verbose);

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

return rootCommand.Parse(args).Invoke();

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

static TestSelectionResult Evaluate(
    TestSelectorConfig config,
    List<string> changedFiles,
    string solution,
    string fromRef,
    string? toRef,
    string workingDir,
    bool verbose)
{
    // No changes
    if (changedFiles.Count == 0)
    {
        var noChangesResult = TestSelectionResult.NoChanges();
        InitializeCategories(noChangesResult, config);
        return noChangesResult;
    }

    // Step 1: Filter ignored files
    var ignoreFilter = new IgnorePathFilter(config.IgnorePaths);
    var (ignoredFiles, activeFiles) = ignoreFilter.SplitFiles(changedFiles);

    if (verbose)
    {
        Console.WriteLine($"Ignored {ignoredFiles.Count} files, {activeFiles.Count} active files");
    }

    // All files ignored
    if (activeFiles.Count == 0)
    {
        var ignoredResult = TestSelectionResult.AllIgnored(ignoredFiles);
        InitializeCategories(ignoredResult, config);
        return ignoredResult;
    }

    // Step 2: Check for critical files (triggerAll categories)
    var criticalDetector = CriticalFileDetector.FromCategories(config.Categories);
    var (criticalFile, criticalPattern) = criticalDetector.FindFirstCriticalFile(activeFiles);

    if (criticalFile != null)
    {
        if (verbose)
        {
            Console.WriteLine($"Critical file detected: {criticalFile} (pattern: {criticalPattern})");
        }

        var criticalResult = TestSelectionResult.CriticalPath(criticalFile, criticalPattern!);
        criticalResult.ChangedFiles = activeFiles;
        criticalResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(criticalResult, config, allEnabled: true);
        return criticalResult;
    }

    // Step 3a: Match files to categories via triggerPaths
    var categoryMapper = new CategoryMapper(config.Categories);
    var (pathTriggeredCategories, pathMatchedFiles) = categoryMapper.GetCategoriesTriggeredByFiles(activeFiles);

    if (verbose)
    {
        var triggeredCategoryNames = pathTriggeredCategories.Where(c => c.Value).Select(c => c.Key);
        Console.WriteLine($"Path-triggered categories: {string.Join(", ", triggeredCategoryNames)}");
        Console.WriteLine($"Path-matched files: {pathMatchedFiles.Count}");
    }

    // Step 3b: Run dotnet-affected
    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedTask = affectedRunner.RunAsync(fromRef, toRef);
    affectedTask.Wait();
    var affectedResult = affectedTask.Result;

    List<string> affectedProjects = [];
    HashSet<string> dotnetMatchedFiles = [];

    if (affectedResult.Success)
    {
        affectedProjects = affectedResult.AffectedProjects;
        // Consider all files in the solution as "matched" by dotnet-affected
        // This is an approximation - ideally we'd track which files led to which affected projects
        dotnetMatchedFiles = GetFilesInSolution(activeFiles, solutionPath, workingDir);

        if (verbose)
        {
            Console.WriteLine($"dotnet-affected found {affectedProjects.Count} affected projects");
            Console.WriteLine($"Files matched by solution: {dotnetMatchedFiles.Count}");
        }
    }
    else
    {
        if (verbose)
        {
            Console.WriteLine($"dotnet-affected failed: {affectedResult.Error}");
        }

        // CONSERVATIVE FALLBACK: dotnet-affected failed, run ALL tests
        var fallbackResult = TestSelectionResult.RunAll($"dotnet-affected failed: {affectedResult.Error}");
        fallbackResult.ChangedFiles = activeFiles;
        fallbackResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(fallbackResult, config, allEnabled: true);
        return fallbackResult;
    }

    // Step 3c: Apply projectMappings
    var projectMappingResolver = new ProjectMappingResolver(config.ProjectMappings);
    var mappedProjects = projectMappingResolver.ResolveAllTestProjects(activeFiles);
    var mappingMatchedFiles = activeFiles.Where(projectMappingResolver.Matches).ToHashSet();

    if (verbose)
    {
        Console.WriteLine($"ProjectMappings resolved {mappedProjects.Count} test projects");
        Console.WriteLine($"Files matched by projectMappings: {mappingMatchedFiles.Count}");
    }

    // Step 4: Conservative fallback - check for unmatched files
    var allMatchedFiles = pathMatchedFiles
        .Union(dotnetMatchedFiles)
        .Union(mappingMatchedFiles)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var unmatchedFiles = activeFiles.Where(f => !allMatchedFiles.Contains(f.Replace('\\', '/'))).ToList();

    if (unmatchedFiles.Count > 0)
    {
        if (verbose)
        {
            Console.WriteLine($"Unmatched files found: {string.Join(", ", unmatchedFiles.Take(5))}...");
        }

        // CONSERVATIVE FALLBACK: unmatched files, run ALL tests
        var reason = unmatchedFiles.Count <= 5
            ? $"Unmatched files: {string.Join(", ", unmatchedFiles)}"
            : $"Unmatched files ({unmatchedFiles.Count}): {string.Join(", ", unmatchedFiles.Take(5))}...";

        var fallbackResult = TestSelectionResult.RunAll(reason);
        fallbackResult.ChangedFiles = activeFiles;
        fallbackResult.IgnoredFiles = ignoredFiles;
        InitializeCategories(fallbackResult, config, allEnabled: true);
        return fallbackResult;
    }

    // Step 5: Filter to test projects from dotnet-affected
    var projectFilter = new TestProjectFilter(workingDir);
    var (testProjects, sourceProjects) = projectFilter.SplitProjects(affectedProjects);

    if (verbose)
    {
        Console.WriteLine($"Affected: {testProjects.Count} test projects, {sourceProjects.Count} source projects");
    }

    // Step 6: Check NuGet-dependent tests
    var nugetChecker = NuGetDependencyChecker.CreateDefault(projectFilter);
    var nugetInfo = nugetChecker.Check(sourceProjects);

    if (verbose && nugetInfo.Triggered)
    {
        Console.WriteLine($"NuGet-dependent tests triggered: {nugetInfo.Reason}");
    }

    // Step 7: Combine test projects from dotnet-affected + projectMappings + NuGet checker
    var allTestProjects = testProjects
        .Concat(mappedProjects)
        .Concat(nugetInfo.Triggered ? nugetInfo.Projects : [])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    // Step 8: Build the result with category flags
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
