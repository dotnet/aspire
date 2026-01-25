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
var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };

var rootCommand = new RootCommand("MSBuild-based test selection tool for Aspire")
{
    solutionOption,
    configOption,
    fromOption,
    toOption,
    changedFilesOption,
    outputOption,
    verboseOption
};

rootCommand.SetAction(result =>
{
    var solution = result.GetValue(solutionOption) ?? "Aspire.slnx";
    var configPath = result.GetValue(configOption) ?? "eng/scripts/test-selector-config.json";
    var fromRef = result.GetValue(fromOption) ?? "origin/main";
    var toRef = result.GetValue(toOption);
    var changedFilesStr = result.GetValue(changedFilesOption);
    var outputPath = result.GetValue(outputOption);
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
    catch (Exception ex)
    {
        var errorResult = TestSelectionResult.WithError(ex.Message);
        Console.WriteLine(errorResult.ToJson());
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

    // Step 2: Check for critical files
    var criticalDetector = new CriticalFileDetector(config.TriggerAllPaths, config.TriggerAllExclude);
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

    // Step 3: Check non-.NET rules (extension/**, playground/**)
    var nonDotNetHandler = new NonDotNetRulesHandler(config.NonDotNetRules);
    var nonDotNetCategories = nonDotNetHandler.GetAllTriggeredCategories(activeFiles);

    if (verbose && nonDotNetCategories.Count > 0)
    {
        Console.WriteLine($"Non-.NET rules triggered categories: {string.Join(", ", nonDotNetCategories.Keys)}");
    }

    // Step 4: Run dotnet-affected
    var solutionPath = Path.IsPathRooted(solution) ? solution : Path.Combine(workingDir, solution);
    var affectedRunner = new DotNetAffectedRunner(solutionPath, workingDir, verbose);
    var affectedTask = affectedRunner.RunAsync(fromRef, toRef);
    affectedTask.Wait();
    var affectedResult = affectedTask.Result;

    List<string> affectedProjects = [];
    if (affectedResult.Success)
    {
        affectedProjects = affectedResult.AffectedProjects;
        if (verbose)
        {
            Console.WriteLine($"dotnet-affected found {affectedProjects.Count} affected projects");
        }
    }
    else
    {
        if (verbose)
        {
            Console.WriteLine($"dotnet-affected failed: {affectedResult.Error}");
            Console.WriteLine("Continuing with non-.NET rules only");
        }
    }

    // Step 5: Filter to test projects
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

    // Step 7: Map projects to categories
    var categoryMapper = new CategoryMapper(config.Categories);
    var categoryFlags = categoryMapper.GetTriggeredCategories(testProjects);

    // Apply non-.NET category triggers
    foreach (var category in nonDotNetCategories.Keys)
    {
        categoryFlags[category] = true;
    }

    // Add NuGet-dependent test projects to the list
    var allTestProjects = new List<string>(testProjects);
    if (nugetInfo.Triggered)
    {
        foreach (var project in nugetInfo.Projects)
        {
            if (!allTestProjects.Contains(project, StringComparer.OrdinalIgnoreCase))
            {
                allTestProjects.Add(project);
            }
        }

        // Mark their categories as triggered
        var nugetCategories = categoryMapper.GetTriggeredCategories(nugetInfo.Projects);
        foreach (var (category, triggered) in nugetCategories)
        {
            if (triggered)
            {
                categoryFlags[category] = true;
            }
        }
    }

    // Build the result
    var result = new TestSelectionResult
    {
        RunAllTests = false,
        Reason = "msbuild_analysis",
        ChangedFiles = activeFiles,
        IgnoredFiles = ignoredFiles,
        DotnetAffectedProjects = affectedProjects,
        AffectedTestProjects = allTestProjects,
        Categories = categoryFlags,
        NuGetDependentTests = nugetInfo.Triggered ? nugetInfo : null
    };

    // Set integrations projects separately for matrix builds
    var integrationProjects = categoryMapper.GroupByCategory(testProjects)
        .GetValueOrDefault("integrations", []);
    result.IntegrationsProjects = integrationProjects;

    return result;
}

static void InitializeCategories(TestSelectionResult result, TestSelectorConfig config, bool allEnabled = false)
{
    result.Categories = [];
    foreach (var categoryName in config.Categories.Keys)
    {
        result.Categories[categoryName] = allEnabled;
    }
}
