// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Input;
using Hex1b.Terminal.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests.Helpers;

/// <summary>
/// Context passed to custom sequence callbacks, providing access to the
/// underlying Hex1b sequence builder and terminal session.
/// </summary>
public sealed class AspireCliAutomationContext
{
    internal AspireCliAutomationContext(
        Hex1bTerminalInputSequenceBuilder sequenceBuilder,
        AspireTerminalSession session)
    {
        SequenceBuilder = sequenceBuilder;
        Session = session;
    }

    /// <summary>
    /// The underlying Hex1b sequence builder for adding custom automation steps.
    /// </summary>
    public Hex1bTerminalInputSequenceBuilder SequenceBuilder { get; }

    /// <summary>
    /// The terminal session containing the terminal, process, and recorder.
    /// </summary>
    public AspireTerminalSession Session { get; }
}

/// <summary>
/// Fluent builder for creating Aspire CLI automation sequences.
/// Provides high-level methods for common CLI operations.
/// </summary>
public sealed class AspireCliAutomationBuilder : IAsyncDisposable
{
    private readonly AspireTerminalSession _session;
    private readonly Hex1bTerminalInputSequenceBuilder _sequenceBuilder;
    private readonly ITestOutputHelper? _output;

    private AspireCliAutomationBuilder(AspireTerminalSession session, ITestOutputHelper? output)
    {
        _session = session;
        _sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();
        _output = output;
    }

    /// <summary>
    /// Writes a test log message and flushes the recording.
    /// This is a convenience method that passes the session's recorder.
    /// </summary>
    private void WriteLog(Hex1bTerminalInputSequenceBuilder builder, string message)
    {
        builder.WriteTestLog(_output, message, _session.Recorder);
    }

    /// <summary>
    /// Adds verification that the last command succeeded to the sequence builder.
    /// Uses the custom prompt format set up by <see cref="PrepareEnvironment"/>: [N OK] or [N ERR:code].
    /// </summary>
    private static void AddCommandVerification(Hex1bTerminalInputSequenceBuilder builder)
    {
        // Small wait to ensure the prompt has rendered
        builder.Wait(TimeSpan.FromMilliseconds(100));

        // Use WaitUntil with a predicate that validates the last command and returns true,
        // or throws an exception if the last command failed
        builder.WaitUntil(
            snapshot =>
            {
                // Use CellPatternSearcher to find all prompts in the format [N OK] or [N ERR:code]
                var searcher = new CellPatternSearcher()
                    .Find(c => c.X == 0 && c.Cell.Character == "[")
                    .BeginCapture("seqno")
                        .RightWhile(c => char.IsNumber(c.Cell.Character[0]))
                    .EndCapture()
                    .Right(' ')
                    .ThenEither(
                        ok => ok.RightText("OK]"),
                        err => err.RightText("ERR:")
                            .BeginCapture("exitcode")
                                .RightWhile(c => char.IsNumber(c.Cell.Character[0]))
                            .EndCapture()
                            .Right(']')
                    );

                var results = searcher.Search(snapshot);

                if (results.Matches.Count == 0)
                {
                    // No prompts found yet - keep waiting (the command is still running)
                    return false;
                }

                // Find the match with the highest sequence number
                var highestMatch = results.Matches
                    .Select(m => new
                    {
                        Match = m,
                        SeqNo = int.TryParse(m.GetCaptureText("seqno"), out var n) ? n : 0,
                        ExitCode = m.GetCaptureText("exitcode")
                    })
                    .OrderByDescending(x => x.SeqNo)
                    .First();

                // Check if it's an error
                if (!string.IsNullOrEmpty(highestMatch.ExitCode))
                {
                    throw new CommandExecutionException(
                        $"Command #{highestMatch.SeqNo} failed with exit code {highestMatch.ExitCode}",
                        highestMatch.SeqNo,
                        int.TryParse(highestMatch.ExitCode, out var code) ? code : -1);
                }

                // Success - return true to indicate we're done waiting
                return true;
            },
            TimeSpan.FromSeconds(30),
            "Verifying last command succeeded");
    }

    /// <summary>
    /// Creates a new automation builder with a configured terminal session.
    /// </summary>
    /// <param name="workingDirectory">Working directory for the terminal.</param>
    /// <param name="recordingName">Name of the test for the recording file (automatically placed in test results).</param>
    /// <param name="output">Optional test output helper for logging.</param>
    /// <param name="prNumber">Optional PR number for the recording title.</param>
    /// <returns>A configured automation builder.</returns>
    public static async Task<AspireCliAutomationBuilder> CreateAsync(
        string workingDirectory,
        string recordingName,
        ITestOutputHelper? output = null,
        int? prNumber = null)
    {
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(recordingName);
        output?.WriteLine($"Recording path: {recordingPath}");

        var title = prNumber.HasValue
            ? $"Aspire CLI Test (PR #{prNumber})"
            : "Aspire CLI Test";

        var session = await CliE2ETestHelpers.CreateTerminalSessionAsync(new AspireTerminalOptions
        {
            WorkingDirectory = workingDirectory,
            RecordingPath = recordingPath,
            RecordingTitle = title
        });

        output?.WriteLine("Terminal started, beginning automation sequence...");

        return new AspireCliAutomationBuilder(session, output);
    }

    /// <summary>
    /// Prepares the shell environment with a custom prompt that tracks command count and exit status.
    /// This is useful for debugging recordings. The prompt format is:
    /// [N OK] $ (success) or [N ERR:code] $ (failure)
    /// Works on both bash (Linux/macOS) and PowerShell (Windows).
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder PrepareEnvironment()
    {
        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, "Preparing shell environment with command tracking prompt...");

            if (OperatingSystem.IsWindows())
            {
                // PowerShell prompt setup
                const string promptSetup = "$global:CMDCOUNT=0; function prompt { $s=$?; $global:CMDCOUNT++; \"[$global:CMDCOUNT $(if($s){'OK'}else{\"ERR:$LASTEXITCODE\"})] PS> \" }";

                ctx.SequenceBuilder
                    .Type(promptSetup)
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));
            }
            else
            {
                // Bash prompt setup
                const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";

                ctx.SequenceBuilder
                    .Type(promptSetup)
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));
            }
        });
    }

    /// <summary>
    /// Runs diagnostic commands to gather environment information for debugging CI failures.
    /// This is useful when project creation hangs to identify NuGet or SDK issues.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for each command (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder RunDiagnostics(TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, "Running diagnostic commands...");

            // List NuGet sources
            WriteLog(ctx.SequenceBuilder, "Checking NuGet sources...");
            if (OperatingSystem.IsWindows())
            {
                ctx.SequenceBuilder
                    .Type("dotnet nuget list source")
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains("Registered Sources", StringComparison.OrdinalIgnoreCase)
                            || snapshot.GetScreenText().Contains("PS>", StringComparison.OrdinalIgnoreCase),
                        effectiveTimeout);
            }
            else
            {
                ctx.SequenceBuilder
                    .Type("dotnet nuget list source")
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains("Registered Sources", StringComparison.OrdinalIgnoreCase)
                            || snapshot.GetScreenText().Contains("$ ", StringComparison.OrdinalIgnoreCase),
                        effectiveTimeout);
            }

            AddCommandVerification(ctx.SequenceBuilder);

            // List installed SDKs
            WriteLog(ctx.SequenceBuilder, "Checking installed .NET SDKs...");
            if (OperatingSystem.IsWindows())
            {
                ctx.SequenceBuilder
                    .Type("dotnet --list-sdks")
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains("PS>", StringComparison.OrdinalIgnoreCase),
                        effectiveTimeout);
            }
            else
            {
                ctx.SequenceBuilder
                    .Type("dotnet --list-sdks")
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains("$ ", StringComparison.OrdinalIgnoreCase),
                        effectiveTimeout);
            }

            AddCommandVerification(ctx.SequenceBuilder);

            WriteLog(ctx.SequenceBuilder, "Diagnostics complete.");
        });
    }

    /// <summary>
    /// Installs the Aspire CLI from a specific pull request's build artifacts.
    /// Uses the appropriate installation script for the current platform.
    /// When running locally (not in CI), uses an echo command for testing.
    /// </summary>
    /// <param name="prNumber">The PR number to download.</param>
    /// <param name="timeout">Maximum time to wait for installation (default: 10 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder InstallAspireCliFromPullRequest(int prNumber, TimeSpan? timeout = null)
    {
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(10);

        return AddSequence(ctx =>
        {
            if (isCI)
            {
                WriteLog(ctx.SequenceBuilder, $"Installing Aspire CLI from PR #{prNumber}...");

                string command;
                if (OperatingSystem.IsWindows())
                {
                    // PowerShell installation command
                    command = $"iex \"& {{ $(irm https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.ps1) }} {prNumber}\"";
                }
                else
                {
                    // Bash installation command
                    command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";
                }

                ctx.SequenceBuilder
                    .Type(command)
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains("Aspire CLI successfully installed to:", StringComparison.OrdinalIgnoreCase),
                        effectiveTimeout);

                AddCommandVerification(ctx.SequenceBuilder);
            }
            else
            {
                WriteLog(ctx.SequenceBuilder, $"[LOCAL] Simulating Aspire CLI install from PR #{prNumber}...");

                // Local testing - just echo
                var echoCommand = OperatingSystem.IsWindows()
                    ? $"Write-Host '[LOCAL] Would install Aspire CLI from PR #{prNumber}'"
                    : $"echo '[LOCAL] Would install Aspire CLI from PR #{prNumber}'";

                ctx.SequenceBuilder
                    .Type(echoCommand)
                    .Enter()
                    .Wait(TimeSpan.FromMilliseconds(500));

                AddCommandVerification(ctx.SequenceBuilder);
            }
        });
    }

    /// <summary>
    /// Sources the Aspire CLI environment to make the 'aspire' command available.
    /// On Linux/macOS, this sources ~/.bashrc. On Windows, this is a no-op as
    /// the PowerShell installer modifies the PATH directly in the current session.
    /// Also explicitly sets ASPIRE_PLAYGROUND=true to enable interactive mode in CI,
    /// and sets .NET CLI environment variables to suppress telemetry and first-time experience.
    /// When running locally (not in CI), uses an echo command for testing.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder SourceAspireCliEnvironment()
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows, the PowerShell installer already updates the current session's PATH
            // But we still need to set ASPIRE_PLAYGROUND for interactive mode and .NET CLI vars
            return AddSequence(ctx =>
            {
                WriteLog(ctx.SequenceBuilder, "Setting environment variables for interactive mode and .NET CLI on Windows...");

                // Set all environment variables: ASPIRE_PLAYGROUND and .NET CLI settings
                ctx.SequenceBuilder
                    .Type("$env:ASPIRE_PLAYGROUND='true'; $env:DOTNET_CLI_TELEMETRY_OPTOUT='true'; $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='true'; $env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'")
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));

                AddCommandVerification(ctx.SequenceBuilder);
            });
        }

        var isCI = CliE2ETestHelpers.IsRunningInCI;

        return AddSequence(ctx =>
        {
            if (isCI)
            {
                WriteLog(ctx.SequenceBuilder, "Sourcing ~/.bashrc and setting environment variables for interactive mode and .NET CLI...");

                // Source bashrc first, then export all environment variables
                // ASPIRE_PLAYGROUND enables interactive mode
                // .NET CLI vars suppress telemetry and first-time experience which can cause hangs
                ctx.SequenceBuilder
                    .Type("source ~/.bashrc && export ASPIRE_PLAYGROUND=true DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false")
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));

                AddCommandVerification(ctx.SequenceBuilder);
            }
            else
            {
                WriteLog(ctx.SequenceBuilder, "[LOCAL] Setting environment variables for interactive mode and .NET CLI...");

                // Even locally, set all environment variables
                ctx.SequenceBuilder
                    .Type("export ASPIRE_PLAYGROUND=true DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false")
                    .Enter()
                    .Wait(TimeSpan.FromMilliseconds(500));

                AddCommandVerification(ctx.SequenceBuilder);
            }
        });
    }

    /// <summary>
    /// Verifies the Aspire CLI installation by checking the version contains the expected commit SHA.
    /// The version format uses 'g' prefix + 8 character SHA (e.g., g6077e9db).
    /// When running locally (not in CI), uses an echo command for testing.
    /// </summary>
    /// <param name="expectedCommitSha">The full commit SHA (will be formatted as g + 8 chars).</param>
    /// <param name="timeout">Maximum time to wait (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder VerifyAspireCliVersion(string expectedCommitSha, TimeSpan? timeout = null)
    {
        // Version format is 'g' prefix + 8 character SHA (matching ci.yml version suffix format)
        var shortSha = expectedCommitSha.Length > 8 ? expectedCommitSha[..8] : expectedCommitSha;
        var versionSha = $"g{shortSha}";
        var isCI = CliE2ETestHelpers.IsRunningInCI;

        return AddSequence(ctx =>
        {
            if (isCI)
            {
                WriteLog(ctx.SequenceBuilder,
                    $"Verifying Aspire CLI version contains commit SHA.\n" +
                    $"  Full SHA:    {expectedCommitSha}\n" +
                    $"  Short SHA:   {shortSha}\n" +
                    $"  Version SHA: {versionSha} (searching for this)");

                ctx.SequenceBuilder
                    .Type("aspire --version")
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains(versionSha, StringComparison.OrdinalIgnoreCase),
                        timeout ?? TimeSpan.FromSeconds(30));

                AddCommandVerification(ctx.SequenceBuilder);
            }
            else
            {
                WriteLog(ctx.SequenceBuilder, $"[LOCAL] Simulating version check for SHA: {versionSha}...");

                // Local testing - just echo
                var echoCommand = OperatingSystem.IsWindows()
                    ? $"Write-Host '[LOCAL] Would verify aspire --version contains {versionSha}'"
                    : $"echo '[LOCAL] Would verify aspire --version contains {versionSha}'";

                ctx.SequenceBuilder
                    .Type(echoCommand)
                    .Enter()
                    .Wait(TimeSpan.FromMilliseconds(500));

                AddCommandVerification(ctx.SequenceBuilder);
            }
        });
    }

    /// <summary>
    /// Creates a new Aspire Starter project using 'aspire new aspire-starter'.
    /// Prompts: "Enter the output path" → "Do you want to create a test project?" (No)
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="debug">If true, adds --debug flag for verbose output.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspireStarterProject(string projectName, bool debug = false, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);
        var debugFlag = debug ? " --debug" : "";

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire Starter project: {projectName}...{(debug ? " (debug mode)" : "")}");

            ctx.SequenceBuilder
                .Type($"aspire{debugFlag} new aspire-starter --name {projectName}")
                .Enter();

            // Wait for "Enter the output path" prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Detected output path prompt, accepting default...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for "Do you want to create a test project?" prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("test project", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Detected test project prompt, selecting No...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire Starter project created successfully!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire TypeScript/C# Starter project using 'aspire new aspire-ts-cs-starter'.
    /// Prompts: "Enter the output path" only
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="debug">If true, adds --debug flag for verbose output.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspireTypeScriptCSharpStarterProject(string projectName, bool debug = false, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);
        var debugFlag = debug ? " --debug" : "";

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire TypeScript/C# Starter project: {projectName}...{(debug ? " (debug mode)" : "")}");

            ctx.SequenceBuilder
                .Type($"aspire{debugFlag} new aspire-ts-cs-starter --name {projectName}")
                .Enter();

            // Wait for "Enter the output path" prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Detected output path prompt, accepting default...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire TypeScript/C# Starter project created successfully!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire Python Starter project using 'aspire new aspire-py-starter'.
    /// Prompts: "Enter the output path" → "Use Redis Cache" (Yes)
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspirePythonStarterProject(string projectName, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire Python Starter project: {projectName}...");

            ctx.SequenceBuilder
                .Type($"aspire new aspire-py-starter --name {projectName}")
                .Enter();

            // Wait for "Enter the output path" prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Detected output path prompt, accepting default...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for "Use Redis Cache" prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Redis Cache", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Detected Redis Cache prompt, accepting default (Yes)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire Python Starter project created successfully!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire AppHost Single-File project using 'aspire new aspire-apphost-singlefile'.
    /// Prompts: "Enter the output path" only
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspireAppHostSingleFileProject(string projectName, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire AppHost Single-File project: {projectName}...");

            ctx.SequenceBuilder
                .Type($"aspire new aspire-apphost-singlefile --name {projectName}")
                .Enter();

            // Wait for "Enter the output path" prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Detected output path prompt, accepting default...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire AppHost Single-File project created successfully!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire Starter project interactively using 'aspire new' without arguments.
    /// Prompts: Select template → Project name → Output path → dev.localhost URLs → Redis Cache → Test project
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspireStarterProjectInteractively(string projectName, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire Starter project interactively: {projectName}...");

            ctx.SequenceBuilder
                .Type("aspire new")
                .Enter();

            // Wait for template selection
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Select a template", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Selecting 'Starter App (ASP.NET Core/Blazor)'...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter(); // First option is selected by default

            // Wait for project name prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("project name", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, $"Entering project name: {projectName}...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Type(projectName).Enter();

            // Wait for output path prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default output path...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for dev.localhost URLs prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("dev.localhost", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default dev.localhost URLs option (No)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for Redis Cache prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Redis Cache", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default Redis Cache option (Yes)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for test project prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("test project", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default test project option (No)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire Starter project created interactively!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire TypeScript/C# Starter project interactively using 'aspire new' without arguments.
    /// Prompts: Select template → Project name → Output path → dev.localhost URLs → Redis Cache
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspireTypeScriptCSharpStarterProjectInteractively(string projectName, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire TypeScript/C# Starter project interactively: {projectName}...");

            ctx.SequenceBuilder
                .Type("aspire new")
                .Enter();

            // Wait for template selection
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Select a template", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Selecting 'Starter App (ASP.NET Core/React)'...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Key(Hex1bKey.DownArrow).Enter(); // Second option

            // Wait for project name prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("project name", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, $"Entering project name: {projectName}...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Type(projectName).Enter();

            // Wait for output path prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default output path...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for dev.localhost URLs prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("dev.localhost", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default dev.localhost URLs option (No)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for Redis Cache prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Redis Cache", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default Redis Cache option (Yes)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire TypeScript/C# Starter project created interactively!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire Python Starter project interactively using 'aspire new' without arguments.
    /// Prompts: Select template → Project name → Output path → dev.localhost URLs → Redis Cache
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspirePythonStarterProjectInteractively(string projectName, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire Python Starter project interactively: {projectName}...");

            ctx.SequenceBuilder
                .Type("aspire new")
                .Enter();

            // Wait for template selection
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Select a template", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Selecting 'Starter App (FastAPI/React)'...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Key(Hex1bKey.DownArrow).Key(Hex1bKey.DownArrow).Enter(); // Third option

            // Wait for project name prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("project name", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, $"Entering project name: {projectName}...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Type(projectName).Enter();

            // Wait for output path prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default output path...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for dev.localhost URLs prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("dev.localhost", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default dev.localhost URLs option (No)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for Redis Cache prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Redis Cache", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default Redis Cache option (Yes)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire Python Starter project created interactively!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Creates a new Aspire AppHost Single-File project interactively using 'aspire new' without arguments.
    /// Prompts: Select template → Project name → Output path
    /// </summary>
    /// <param name="projectName">The name of the project to create.</param>
    /// <param name="timeout">Maximum time to wait for project creation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder CreateAspireAppHostSingleFileProjectInteractively(string projectName, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, $"Creating Aspire AppHost Single-File project interactively: {projectName}...");

            ctx.SequenceBuilder
                .Type("aspire new")
                .Enter();

            // Wait for template selection
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Select a template", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Selecting 'Empty AppHost'...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Key(Hex1bKey.DownArrow).Key(Hex1bKey.DownArrow).Key(Hex1bKey.DownArrow).Enter(); // Fourth option

            // Wait for project name prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("project name", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, $"Entering project name: {projectName}...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Type(projectName).Enter();

            // Wait for output path prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("output path", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default output path...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for dev.localhost URLs prompt
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("dev.localhost", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Accepting default dev.localhost URLs option (No)...");
            ctx.SequenceBuilder.Wait(TimeSpan.FromSeconds(1)).Enter();

            // Wait for completion
            ctx.SequenceBuilder
                .WaitUntil(
                    snapshot => snapshot.GetScreenText().Contains("Project created successfully", StringComparison.OrdinalIgnoreCase),
                    effectiveTimeout);

            WriteLog(ctx.SequenceBuilder, "Aspire AppHost Single-File project created interactively!");

            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Runs an Aspire project by navigating to its directory and executing 'aspire run'.
    /// </summary>
    /// <param name="projectName">The name of the project (used to determine the directory path).</param>
    /// <param name="isFlatStructure">True for projects where apphost.cs is directly in the project folder (e.g., single-file, Python templates).</param>
    /// <param name="timeout">Maximum time to wait for the project to start.</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder RunAspireProject(string projectName, bool isFlatStructure = false, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        return AddSequence(ctx =>
        {
            string cdCommand;
            if (isFlatStructure)
            {
                // Flat structure projects have apphost.cs in the project root
                WriteLog(ctx.SequenceBuilder, $"Navigating to {projectName} (flat structure) and running...");
                cdCommand = OperatingSystem.IsWindows()
                    ? $"cd {projectName}"
                    : $"cd {projectName}";
            }
            else
            {
                WriteLog(ctx.SequenceBuilder, $"Navigating to {projectName}.AppHost and running Aspire project...");
                cdCommand = OperatingSystem.IsWindows()
                    ? $"cd {projectName}\\{projectName}.AppHost"
                    : $"cd {projectName}/{projectName}.AppHost";
            }

            ctx.SequenceBuilder
                .Type(cdCommand)
                .Enter()
                .Wait(TimeSpan.FromSeconds(1));

            AddCommandVerification(ctx.SequenceBuilder);

            WriteLog(ctx.SequenceBuilder, "Starting 'aspire run'...");

            ctx.SequenceBuilder
                .Type("aspire run")
                .Enter()
                .WaitUntil(
                    snapshot =>
                    {
                        var screenText = snapshot.GetScreenText();
                        // Wait for the dashboard URL to appear, indicating the app is running
                        return screenText.Contains("Login to the dashboard at", StringComparison.OrdinalIgnoreCase)
                            || screenText.Contains("Now listening on:", StringComparison.OrdinalIgnoreCase)
                            || screenText.Contains("dashboard", StringComparison.OrdinalIgnoreCase) && screenText.Contains("http", StringComparison.OrdinalIgnoreCase);
                    },
                    effectiveTimeout);
        });
    }

    /// <summary>
    /// Stops a running Aspire project by sending Ctrl+C.
    /// Waits for the process to shut down gracefully.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for the project to stop (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder StopAspireProject(TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);

        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, "Sending Ctrl+C to stop the Aspire project...");

            // Send Ctrl+C to interrupt the running process
            ctx.SequenceBuilder
                .Ctrl().Key(Hex1bKey.C)
                .WaitUntil(
                    snapshot =>
                    {
                        var screenText = snapshot.GetScreenText();
                        // Wait for the prompt to reappear or a shutdown message
                        return screenText.Contains('$')
                            || screenText.Contains("PS>", StringComparison.OrdinalIgnoreCase)
                            || screenText.Contains("Shutdown completed", StringComparison.OrdinalIgnoreCase)
                            || screenText.Contains("Application is shutting down", StringComparison.OrdinalIgnoreCase);
                    },
                    effectiveTimeout);
        });
    }

    /// <summary>
    /// Verifies that the last command executed successfully by checking the shell prompt.
    /// Uses the custom prompt format set up by <see cref="PrepareEnvironment"/>: [N OK] or [N ERR:code].
    /// Throws an exception if the last command failed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder VerifyLastCommandSucceeded()
    {
        return AddSequence(ctx =>
        {
            WriteLog(ctx.SequenceBuilder, "Verifying last command succeeded...");
            AddCommandVerification(ctx.SequenceBuilder);
        });
    }

    /// <summary>
    /// Exits the terminal session cleanly.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder ExitTerminal()
    {
        _sequenceBuilder
            .Type("exit")
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));

        return this;
    }

    /// <summary>
    /// Adds a custom sequence using the underlying Hex1b sequence builder.
    /// Use this for operations not covered by the high-level methods.
    /// </summary>
    /// <param name="configure">A callback to configure the sequence.</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder AddSequence(Action<AspireCliAutomationContext> configure)
    {
        var context = new AspireCliAutomationContext(_sequenceBuilder, _session);
        configure(context);
        return this;
    }

    /// <summary>
    /// Executes the automation sequence with built-in exception handling and assertions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Wait for shell to initialize
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        var sequence = _sequenceBuilder.Build();

        try
        {
            await sequence.ApplyAsync(_session.Terminal, cancellationToken);
            _output?.WriteLine("Automation sequence completed successfully.");
        }
        catch (TimeoutException ex)
        {
            _output?.WriteLine($"Operation timed out: {ex.Message}");

            // Flush recording to ensure we capture the state at timeout
            if (_session.Recorder is not null)
            {
                await _session.Recorder.FlushAsync(CancellationToken.None);
            }

            using var snapshot = _session.Terminal.CreateSnapshot();
            var content = snapshot.GetScreenText();
            _output?.WriteLine("Final terminal content:");
            _output?.WriteLine(content);

            Assert.Fail($"Test timed out. Terminal content:\n{content}");
        }

        // Wait for the process to exit
        var exitCode = await CliE2ETestHelpers.WaitForExitAsync(_session.Process, TimeSpan.FromSeconds(10), cancellationToken);
        _output?.WriteLine($"Terminal process exited with code: {exitCode?.ToString() ?? "killed"}");
    }

    /// <summary>
    /// Gets the path to the asciinema recording file.
    /// </summary>
    public string? RecordingPath => _session.Recorder is not null
        ? CliE2ETestHelpers.GetTestResultsRecordingPath("recording")
        : null;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _session.DisposeAsync();
    }
}

/// <summary>
/// Exception thrown when a command executed in the terminal fails.
/// </summary>
public sealed class CommandExecutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="commandNumber">The sequence number of the failed command.</param>
    /// <param name="exitCode">The exit code of the failed command.</param>
    public CommandExecutionException(string message, int commandNumber, int exitCode)
        : base(message)
    {
        CommandNumber = commandNumber;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Gets the sequence number of the failed command.
    /// </summary>
    public int CommandNumber { get; }

    /// <summary>
    /// Gets the exit code of the failed command.
    /// </summary>
    public int ExitCode { get; }
}
