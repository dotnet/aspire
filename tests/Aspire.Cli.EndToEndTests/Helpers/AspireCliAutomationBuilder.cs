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
    private int _commandSequence;

    private AspireCliAutomationBuilder(AspireTerminalSession session, ITestOutputHelper? output)
    {
        _session = session;
        _sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();
        _output = output;
        _commandSequence = 0;
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
    /// This should be called first. The prompt format is:
    /// [N ✔] $ (success) or [N ✘:code] $ (failure)
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder PrepareEnvironment()
    {
        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo ✔ || echo ✘:$s)] \\$ \"'";

        _sequenceBuilder
            .Type(promptSetup)
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));

        _commandSequence++;
        return WaitForSequence(_commandSequence);
    }

    /// <summary>
    /// Waits for a specific command sequence number to appear in the prompt.
    /// If the command succeeded (✔), returns normally. If it failed (✘), throws an exception.
    /// </summary>
    /// <param name="sequenceNumber">The command sequence number to wait for.</param>
    /// <param name="timeout">Maximum time to wait (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder WaitForSequence(int sequenceNumber, TimeSpan? timeout = null)
    {
        var successPattern = $"[{sequenceNumber} ✔]";
        var failurePattern = $"[{sequenceNumber} ✘";

        _sequenceBuilder.WaitUntil(
            snapshot =>
            {
                var text = snapshot.GetScreenText();

                if (text.Contains(failurePattern, StringComparison.Ordinal))
                {
                    throw new TerminalCommandFailedException(
                        $"Command {sequenceNumber} failed.",
                        snapshot,
                        sequenceNumber);
                }

                return text.Contains(successPattern, StringComparison.Ordinal);
            },
            timeout ?? TimeSpan.FromMinutes(5));

        return this;
    }

    /// <summary>
    /// Downloads and installs the Aspire CLI for a specific PR number.
    /// </summary>
    /// <param name="prNumber">The PR number to download.</param>
    /// <param name="timeout">Maximum time to wait for installation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder DownloadAndInstallAspireCli(int prNumber, TimeSpan? timeout = null)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";

        _sequenceBuilder
            .Type(command)
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains("Successfully added aspire to", StringComparison.OrdinalIgnoreCase),
                timeout ?? TimeSpan.FromMinutes(5));

        _commandSequence++;
        return WaitForSequence(_commandSequence, timeout);
    }

    /// <summary>
    /// Sources the Aspire CLI environment to make the 'aspire' command available.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder SourceAspireCliEnvironment()
    {
        _sequenceBuilder
            .Type("source ~/.bashrc")
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));

        _commandSequence++;
        return WaitForSequence(_commandSequence);
    }

    /// <summary>
    /// Verifies the Aspire CLI installation by checking the version contains the expected commit SHA.
    /// </summary>
    /// <param name="expectedCommitSha">The commit SHA to look for in the version output.</param>
    /// <param name="timeout">Maximum time to wait (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public AspireCliAutomationBuilder VerifyAspireCliVersion(string expectedCommitSha, TimeSpan? timeout = null)
    {
        _sequenceBuilder
            .Type("aspire --version")
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains(expectedCommitSha, StringComparison.OrdinalIgnoreCase),
                timeout ?? TimeSpan.FromSeconds(30));

        _commandSequence++;
        return WaitForSequence(_commandSequence);
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
        catch (TerminalCommandFailedException ex)
        {
            _output?.WriteLine($"Command {ex.CommandSequence} failed.");
            _output?.WriteLine("Terminal content:");
            _output?.WriteLine(ex.TerminalContent);

            Assert.Fail($"Command {ex.CommandSequence} failed. Terminal content:\n{ex.TerminalContent}");
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
