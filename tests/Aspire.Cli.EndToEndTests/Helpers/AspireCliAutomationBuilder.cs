// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// [N ✔] $ (success) or [N ✘:code] $ (failure)
    /// Works on both bash (Linux/macOS) and PowerShell (Windows).
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder PrepareEnvironment()
    {
        _output?.WriteLine("Preparing shell environment with command tracking prompt...");

        return AddSequence(ctx =>
        {
            if (OperatingSystem.IsWindows())
            {
                // PowerShell prompt setup
                const string promptSetup = "$global:CMDCOUNT=0; function prompt { $s=$?; $global:CMDCOUNT++; \"[$global:CMDCOUNT $(if($s){'✔'}else{\"✘:$LASTEXITCODE\"})] PS> \" }";

                ctx.SequenceBuilder
                    .Type(promptSetup)
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));
            }
            else
            {
                // Bash prompt setup
                const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo ✔ || echo ✘:$s)] \\$ \"'";

                ctx.SequenceBuilder
                    .Type(promptSetup)
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));
            }
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

        if (isCI)
        {
            _output?.WriteLine($"Installing Aspire CLI from PR #{prNumber}...");
        }
        else
        {
            _output?.WriteLine($"[LOCAL] Simulating Aspire CLI install from PR #{prNumber}...");
        }

        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(10);

        return AddSequence(ctx =>
        {
            if (isCI)
            {
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
            }
            else
            {
                // Local testing - just echo
                var echoCommand = OperatingSystem.IsWindows()
                    ? $"Write-Host '[LOCAL] Would install Aspire CLI from PR #{prNumber}'"
                    : $"echo '[LOCAL] Would install Aspire CLI from PR #{prNumber}'";

                ctx.SequenceBuilder
                    .Type(echoCommand)
                    .Enter()
                    .Wait(TimeSpan.FromMilliseconds(500));
            }
        });
    }

    /// <summary>
    /// Sources the Aspire CLI environment to make the 'aspire' command available.
    /// On Linux/macOS, this sources ~/.bashrc. On Windows, this is a no-op as
    /// the PowerShell installer modifies the PATH directly in the current session.
    /// When running locally (not in CI), uses an echo command for testing.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder SourceAspireCliEnvironment()
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows, the PowerShell installer already updates the current session's PATH
            _output?.WriteLine("Skipping environment sourcing on Windows (PATH already updated)...");
            return this;
        }

        var isCI = CliE2ETestHelpers.IsRunningInCI;

        if (isCI)
        {
            _output?.WriteLine("Sourcing ~/.bashrc to add Aspire CLI to PATH...");
        }
        else
        {
            _output?.WriteLine("[LOCAL] Simulating environment sourcing...");
        }

        return AddSequence(ctx =>
        {
            if (isCI)
            {
                ctx.SequenceBuilder
                    .Type("source ~/.bashrc")
                    .Enter()
                    .Wait(TimeSpan.FromSeconds(1));
            }
            else
            {
                ctx.SequenceBuilder
                    .Type("echo '[LOCAL] Would source ~/.bashrc'")
                    .Enter()
                    .Wait(TimeSpan.FromMilliseconds(500));
            }
        });
    }

    /// <summary>
    /// Verifies the Aspire CLI installation by checking the version contains the expected commit SHA.
    /// The commit SHA is trimmed to the first 9 characters for matching.
    /// When running locally (not in CI), uses an echo command for testing.
    /// </summary>
    /// <param name="expectedCommitSha">The full commit SHA (will be trimmed to 9 characters).</param>
    /// <param name="timeout">Maximum time to wait (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder VerifyAspireCliVersion(string expectedCommitSha, TimeSpan? timeout = null)
    {
        // Use first 9 characters of the commit SHA for matching
        var shortSha = expectedCommitSha.Length > 9 ? expectedCommitSha[..9] : expectedCommitSha;
        var isCI = CliE2ETestHelpers.IsRunningInCI;

        if (isCI)
        {
            _output?.WriteLine($"Verifying Aspire CLI version contains commit SHA: {shortSha}...");
        }
        else
        {
            _output?.WriteLine($"[LOCAL] Simulating version check for SHA: {shortSha}...");
        }

        return AddSequence(ctx =>
        {
            if (isCI)
            {
                ctx.SequenceBuilder
                    .Type("aspire --version")
                    .Enter()
                    .WaitUntil(
                        snapshot => snapshot.GetScreenText().Contains(shortSha, StringComparison.OrdinalIgnoreCase),
                        timeout ?? TimeSpan.FromSeconds(30));
            }
            else
            {
                // Local testing - just echo
                var echoCommand = OperatingSystem.IsWindows()
                    ? $"Write-Host '[LOCAL] Would verify aspire --version contains {shortSha}'"
                    : $"echo '[LOCAL] Would verify aspire --version contains {shortSha}'";

                ctx.SequenceBuilder
                    .Type(echoCommand)
                    .Enter()
                    .Wait(TimeSpan.FromMilliseconds(500));
            }
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
