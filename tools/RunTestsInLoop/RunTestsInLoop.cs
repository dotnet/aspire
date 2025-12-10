// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// RunTestsInLoop - A utility to run tests repeatedly to reproduce intermittent failures/hangs
//
// This tool helps identify flaky tests by running a specified test project or test method
// multiple times in a loop. It's particularly useful for reproducing hangs that occur
// intermittently in CI runs.
//
// Usage examples:
//   # Run DistributedApplicationTests 10 times with 5 minute timeout per run
//   dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --iterations 10 --timeout 5
//
//   # Run a specific test method 50 times
//   dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --method "DistributedApplicationTests.RegisteredLifecycleHookIsExecutedWhenRunAsynchronously" --iterations 50
//
//   # Run a specific test class 20 times
//   dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --class "Aspire.Hosting.Tests.DistributedApplicationTests" --iterations 20
//
//   # Run with verbose output
//   dotnet run --project tools/RunTestsInLoop -- --project tests/Aspire.Hosting.Tests --iterations 5 --verbose
//

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text;

var rootCommand = new RootCommand("Run tests in a loop to reproduce intermittent failures or hangs");

var projectOption = new Option<string>("--project", "-p")
{
    Description = "Path to the test project (relative to repo root or absolute)",
    Required = true
};

var iterationsOption = new Option<int>("--iterations", "-i")
{
    Description = "Number of times to run the tests",
    DefaultValueFactory = _ => 10
};

var timeoutOption = new Option<int>("--timeout", "-t")
{
    Description = "Timeout in minutes for each test run (0 = no timeout)",
    DefaultValueFactory = _ => 10
};

var methodOption = new Option<string?>("--method", "-m")
{
    Description = "Test method name to filter (short name, uses wildcard prefix to match any class)"
};

var classOption = new Option<string?>("--class", "-c")
{
    Description = "Fully-qualified test class name to filter (e.g., Namespace.Class)"
};

var namespaceOption = new Option<string?>("--namespace", "-n")
{
    Description = "Test namespace to filter"
};

var verboseOption = new Option<bool>("--verbose", "-v")
{
    Description = "Show detailed output from test runs"
};

var stopOnFailureOption = new Option<bool>("--stop-on-failure", "-s")
{
    Description = "Stop running after the first failure or hang",
    DefaultValueFactory = _ => true
};

var extraArgsOption = new Option<string?>("--extra-args", "-e")
{
    Description = "Additional arguments to pass to dotnet test"
};

var noBuildOption = new Option<bool>("--no-build")
{
    Description = "Skip building the test project (use when already built)"
};

rootCommand.Options.Add(projectOption);
rootCommand.Options.Add(iterationsOption);
rootCommand.Options.Add(timeoutOption);
rootCommand.Options.Add(methodOption);
rootCommand.Options.Add(classOption);
rootCommand.Options.Add(namespaceOption);
rootCommand.Options.Add(verboseOption);
rootCommand.Options.Add(stopOnFailureOption);
rootCommand.Options.Add(extraArgsOption);
rootCommand.Options.Add(noBuildOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var project = parseResult.GetValue(projectOption)!;
    var iterations = parseResult.GetValue(iterationsOption);
    var timeout = parseResult.GetValue(timeoutOption);
    var method = parseResult.GetValue(methodOption);
    var testClass = parseResult.GetValue(classOption);
    var ns = parseResult.GetValue(namespaceOption);
    var verbose = parseResult.GetValue(verboseOption);
    var stopOnFailure = parseResult.GetValue(stopOnFailureOption);
    var extraArgs = parseResult.GetValue(extraArgsOption);
    var noBuild = parseResult.GetValue(noBuildOption);

    return await RunTestsInLoop(project, iterations, timeout, method, testClass, ns, verbose, stopOnFailure, extraArgs, noBuild, cancellationToken).ConfigureAwait(false);
});

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

static async Task<int> RunTestsInLoop(
    string project,
    int iterations,
    int timeoutMinutes,
    string? method,
    string? testClass,
    string? ns,
    bool verbose,
    bool stopOnFailure,
    string? extraArgs,
    bool noBuild,
    CancellationToken cancellationToken)
{
    // Find repo root
    var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
    if (repoRoot == null)
    {
        Console.Error.WriteLine("Could not find repository root (no .git folder found)");
        return 1;
    }

    // Resolve project path
    var projectPath = Path.IsPathRooted(project) ? project : Path.Combine(repoRoot, project);
    if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    {
        // Try to find the .csproj file in the directory
        if (Directory.Exists(projectPath))
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj");
            if (csprojFiles.Length == 1)
            {
                projectPath = csprojFiles[0];
            }
            else if (csprojFiles.Length == 0)
            {
                Console.Error.WriteLine($"No .csproj file found in: {projectPath}");
                return 1;
            }
            else
            {
                Console.Error.WriteLine($"Multiple .csproj files found in: {projectPath}. Please specify the exact project file.");
                return 1;
            }
        }
        else
        {
            Console.Error.WriteLine($"Project path does not exist: {projectPath}");
            return 1;
        }
    }

    if (!File.Exists(projectPath))
    {
        Console.Error.WriteLine($"Project file does not exist: {projectPath}");
        return 1;
    }

    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║              Test Loop Runner for Aspire                     ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"Project:    {Path.GetRelativePath(repoRoot, projectPath)}");
    Console.WriteLine($"Iterations: {iterations}");
    Console.WriteLine($"Timeout:    {(timeoutMinutes > 0 ? $"{timeoutMinutes} minutes" : "No timeout")}");
    if (!string.IsNullOrEmpty(method))
    {
        Console.WriteLine($"Method:     {method}");
    }
    if (!string.IsNullOrEmpty(testClass))
    {
        Console.WriteLine($"Class:      {testClass}");
    }
    if (!string.IsNullOrEmpty(ns))
    {
        Console.WriteLine($"Namespace:  {ns}");
    }
    Console.WriteLine($"Stop on failure: {stopOnFailure}");
    Console.WriteLine();

    // Determine dotnet command based on OS
    var dotnetCommand = OperatingSystem.IsWindows() ? "dotnet.cmd" : "./dotnet.sh";
    var dotnetPath = Path.Combine(repoRoot, dotnetCommand);
    if (!File.Exists(dotnetPath))
    {
        // Fall back to system dotnet
        dotnetCommand = "dotnet";
        dotnetPath = "dotnet";
    }

    // Build the project first if not skipped
    if (!noBuild)
    {
        Console.WriteLine("Building test project...");
        var buildResult = await RunProcess(
            dotnetPath,
            string.Format(CultureInfo.InvariantCulture, "build \"{0}\" --no-restore", projectPath),
            repoRoot,
            verbose,
            TimeSpan.FromMinutes(5),
            cancellationToken).ConfigureAwait(false);

        if (buildResult.ExitCode != 0)
        {
            Console.Error.WriteLine("Build failed!");
            return 1;
        }
        Console.WriteLine("Build succeeded.");
        Console.WriteLine();
    }

    // Prepare test arguments
    var testArgs = new StringBuilder();
    testArgs.Append(CultureInfo.InvariantCulture, $"test \"{projectPath}\" --no-build");

    // Add platform-specific test arguments
    testArgs.Append(" -- --filter-not-trait \"quarantined=true\" --filter-not-trait \"outerloop=true\"");

    if (!string.IsNullOrEmpty(method))
    {
        testArgs.Append(CultureInfo.InvariantCulture, $" --filter-method \"*.{method}\"");
    }
    if (!string.IsNullOrEmpty(testClass))
    {
        testArgs.Append(CultureInfo.InvariantCulture, $" --filter-class \"{testClass}\"");
    }
    if (!string.IsNullOrEmpty(ns))
    {
        testArgs.Append(CultureInfo.InvariantCulture, $" --filter-namespace \"{ns}\"");
    }

    if (!string.IsNullOrEmpty(extraArgs))
    {
        testArgs.Append(CultureInfo.InvariantCulture, $" {extraArgs}");
    }

    var testArgsString = testArgs.ToString();
    Console.WriteLine($"Test command: {dotnetCommand} {testArgsString}");
    Console.WriteLine();

    // Statistics
    var passed = 0;
    var failed = 0;
    var timedOut = 0;
    var runTimes = new List<TimeSpan>();

    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    Starting Test Loop                        ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    for (var i = 1; i <= iterations && !cancellationToken.IsCancellationRequested; i++)
    {
        Console.WriteLine($"┌──────────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│  Iteration {i}/{iterations,-48}│");
        Console.WriteLine($"└──────────────────────────────────────────────────────────────┘");

        var sw = Stopwatch.StartNew();
        var timeout = timeoutMinutes > 0 ? TimeSpan.FromMinutes(timeoutMinutes) : TimeSpan.FromHours(24);

        var result = await RunProcess(dotnetPath, testArgsString, repoRoot, verbose, timeout, cancellationToken).ConfigureAwait(false);
        sw.Stop();
        runTimes.Add(sw.Elapsed);

        if (result.TimedOut)
        {
            timedOut++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ⏱️  TIMEOUT after {sw.Elapsed.TotalMinutes:F1} minutes!");
            Console.ResetColor();

            // Print last part of output for debugging
            if (!verbose && !string.IsNullOrWhiteSpace(result.Output))
            {
                Console.WriteLine("  Last 50 lines of output:");
                var lines = result.Output.Split('\n');
                foreach (var line in lines.TakeLast(50))
                {
                    Console.WriteLine($"    {line}");
                }
            }

            if (stopOnFailure)
            {
                Console.WriteLine();
                Console.WriteLine("  Stopping due to timeout (--stop-on-failure is enabled)");
                break;
            }
        }
        else if (result.ExitCode != 0)
        {
            failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ❌ FAILED (exit code: {result.ExitCode}) in {sw.Elapsed.TotalSeconds:F1}s");
            Console.ResetColor();

            // Print error output for debugging
            if (!verbose && !string.IsNullOrWhiteSpace(result.ErrorOutput))
            {
                Console.WriteLine("  Error output:");
                foreach (var line in result.ErrorOutput.Split('\n').Take(20))
                {
                    Console.WriteLine($"    {line}");
                }
            }

            if (stopOnFailure)
            {
                Console.WriteLine();
                Console.WriteLine("  Stopping due to failure (--stop-on-failure is enabled)");
                break;
            }
        }
        else
        {
            passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✅ PASSED in {sw.Elapsed.TotalSeconds:F1}s");
            Console.ResetColor();
        }

        Console.WriteLine();

        // Print running statistics every 5 iterations or at the end
        if (i % 5 == 0 || i == iterations)
        {
            PrintStatistics(passed, failed, timedOut, runTimes, i);
        }
    }

    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                      Final Results                           ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    PrintStatistics(passed, failed, timedOut, runTimes, passed + failed + timedOut);

    if (failed > 0 || timedOut > 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine("💡 Tip: If you found a flaky test, consider quarantining it:");
        Console.WriteLine("   dotnet run --project tools/QuarantineTools -- -q -i <issue-url> <Namespace.Class.Method>");
        Console.ResetColor();
        return 1;
    }

    return 0;
}

static void PrintStatistics(int passed, int failed, int timedOut, List<TimeSpan> runTimes, int totalRuns)
{
    Console.WriteLine("┌────────────────────────────────────────┐");
    Console.WriteLine("│  Statistics                            │");
    Console.WriteLine("├────────────────────────────────────────┤");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"│  Passed:     {passed,5}                     │");
    Console.ResetColor();
    Console.ForegroundColor = failed > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
    Console.WriteLine($"│  Failed:     {failed,5}                     │");
    Console.ResetColor();
    Console.ForegroundColor = timedOut > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
    Console.WriteLine($"│  Timed out:  {timedOut,5}                     │");
    Console.ResetColor();
    Console.WriteLine($"│  Total:      {totalRuns,5}                     │");

    if (runTimes.Count > 0)
    {
        var avgTime = TimeSpan.FromTicks((long)runTimes.Average(t => t.Ticks));
        var minTime = runTimes.Min();
        var maxTime = runTimes.Max();
        Console.WriteLine("├────────────────────────────────────────┤");
        Console.WriteLine($"│  Avg time:   {avgTime.TotalSeconds,5:F1}s                    │");
        Console.WriteLine($"│  Min time:   {minTime.TotalSeconds,5:F1}s                    │");
        Console.WriteLine($"│  Max time:   {maxTime.TotalSeconds,5:F1}s                    │");
    }

    var successRate = totalRuns > 0 ? (double)passed / totalRuns * 100 : 0;
    Console.WriteLine("├────────────────────────────────────────┤");
    Console.ForegroundColor = successRate >= 100 ? ConsoleColor.Green : successRate >= 90 ? ConsoleColor.Yellow : ConsoleColor.Red;
    Console.WriteLine($"│  Success rate: {successRate,5:F1}%                 │");
    Console.ResetColor();
    Console.WriteLine("└────────────────────────────────────────┘");
}

static string? FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    return null;
}

static async Task<ProcessResult> RunProcess(
    string command,
    string arguments,
    string workingDirectory,
    bool showOutput,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    var outputBuilder = new StringBuilder();
    var errorBuilder = new StringBuilder();

    using var process = new Process();
    process.StartInfo = new ProcessStartInfo
    {
        FileName = command,
        Arguments = arguments,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    process.OutputDataReceived += (sender, e) =>
    {
        if (e.Data != null)
        {
            outputBuilder.AppendLine(e.Data);
            if (showOutput)
            {
                Console.WriteLine(e.Data);
            }
        }
    };

    process.ErrorDataReceived += (sender, e) =>
    {
        if (e.Data != null)
        {
            errorBuilder.AppendLine(e.Data);
            if (showOutput)
            {
                Console.Error.WriteLine(e.Data);
            }
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    linkedCts.CancelAfter(timeout);

    try
    {
        await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        return new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString(), false);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        // Timeout - not user cancellation
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Ignore errors during kill
        }
        return new ProcessResult(-1, outputBuilder.ToString(), errorBuilder.ToString(), true);
    }
}

sealed record ProcessResult(int ExitCode, string Output, string ErrorOutput, bool TimedOut);
