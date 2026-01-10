// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal;
using Hex1b.Terminal.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests.Helpers;

/// <summary>
/// Extension methods for Hex1b terminal automation to simplify Aspire CLI operations.
/// These extend the fluent sequence builder to keep the API concise.
/// </summary>
internal static class AspireCliHex1bExtensions
{
    /// <summary>
    /// Adds a callback step that executes during sequence application.
    /// Use this for logging, assertions, or any side effects that should happen
    /// at execution time rather than build time.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="callback">The action to execute during sequence application.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder Callback(
        this Hex1bTerminalInputSequenceBuilder builder,
        Action callback)
    {
        return builder.WaitUntil(_ =>
        {
            callback();
            return true;
        }, TimeSpan.FromMilliseconds(1));
    }

    /// <summary>
    /// Flushes the asciinema recording to disk.
    /// This ensures that the recording is saved even if the test times out or fails.
    /// Call this periodically during long-running operations.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="recorder">The asciinema recorder to flush.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder FlushRecording(
        this Hex1bTerminalInputSequenceBuilder builder,
        AsciinemaRecorder? recorder)
    {
        if (recorder is null)
        {
            return builder;
        }

        return builder.WaitUntil(_ =>
        {
            recorder.FlushAsync().GetAwaiter().GetResult();
            return true;
        }, TimeSpan.FromMilliseconds(1));
    }

    /// <summary>
    /// Writes a test log message along with the current terminal snapshot.
    /// Use this for debugging and tracing test execution. The log includes
    /// both the message and the current terminal screen content.
    /// Also flushes the recording to ensure it's saved even on timeout/failure.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="output">The test output helper for logging.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="recorder">Optional asciinema recorder to flush after logging.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder WriteTestLog(
        this Hex1bTerminalInputSequenceBuilder builder,
        ITestOutputHelper? output,
        string message,
        AsciinemaRecorder? recorder = null)
    {
        if (output is null && recorder is null)
        {
            return builder;
        }

        return builder.WaitUntil(snapshot =>
        {
            if (output is not null)
            {
                var terminalText = snapshot.GetScreenText();
                output.WriteLine($"[LOG] {message}");
                output.WriteLine($"[TERMINAL]\n{terminalText}");
                output.WriteLine(new string('-', 80));
            }

            // Flush the recording to ensure we capture state even on timeout/failure
            recorder?.FlushAsync().GetAwaiter().GetResult();

            return true;
        }, TimeSpan.FromMilliseconds(1));
    }

    /// <summary>
    /// Prepares the shell environment with a custom prompt that tracks command count and exit status.
    /// This makes it easier to detect when commands complete. The prompt format is:
    /// [N ✔] $ (success) or [N ✘:code] $ (failure)
    /// Works on both bash (Linux/macOS) and PowerShell (Windows).
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder PrepareEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        if (OperatingSystem.IsWindows())
        {
            // PowerShell prompt setup
            const string promptSetup = "$global:CMDCOUNT=0; function prompt { $s=$?; $global:CMDCOUNT++; \"[$global:CMDCOUNT $(if($s){'✔'}else{\"✘:$LASTEXITCODE\"})] PS> \" }";

            return builder
                .Type(promptSetup)
                .Enter()
                .Wait(TimeSpan.FromMilliseconds(500));
        }
        else
        {
            // Bash prompt setup
            const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo ✔ || echo ✘:$s)] \\$ \"'";

            return builder
                .Type(promptSetup)
                .Enter()
                .Wait(TimeSpan.FromMilliseconds(500));
        }
    }

    /// <summary>
    /// Installs the Aspire CLI from a specific pull request's build artifacts.
    /// Uses the appropriate installation script for the current platform.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="prNumber">The PR number to download.</param>
    /// <param name="timeout">Maximum time to wait for installation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder InstallAspireCliFromPullRequest(
        this Hex1bTerminalInputSequenceBuilder builder,
        int prNumber,
        TimeSpan? timeout = null)
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

        return builder
            .Type(command)
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains("Successfully added aspire to", StringComparison.OrdinalIgnoreCase),
                timeout ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Sources the Aspire CLI environment to make the 'aspire' command available in the current shell.
    /// On Linux/macOS, this sources ~/.bashrc which contains the PATH updates from the installer.
    /// On Windows, this is a no-op as the PowerShell installer updates PATH directly.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder SourceAspireCliEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows, the PowerShell installer already updates the current session's PATH
            return builder;
        }

        // The installer adds aspire to ~/.dotnet/tools and updates ~/.bashrc
        // We need to source ~/.bashrc to pick up the PATH changes
        return builder
            .Type("source ~/.bashrc")
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Verifies the Aspire CLI installation by running 'aspire --version' and waiting for the expected commit SHA.
    /// The commit SHA is trimmed to the first 9 characters for matching.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="expectedCommitSha">The full commit SHA (will be trimmed to 9 characters).</param>
    /// <param name="timeout">Maximum time to wait for version output (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder VerifyAspireCliVersion(
        this Hex1bTerminalInputSequenceBuilder builder,
        string expectedCommitSha,
        TimeSpan? timeout = null)
    {
        // Use first 9 characters of the commit SHA for matching
        var shortSha = expectedCommitSha.Length > 9 ? expectedCommitSha[..9] : expectedCommitSha;

        return builder
            .Type("aspire --version")
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains(shortSha, StringComparison.OrdinalIgnoreCase),
                timeout ?? TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Exits the terminal session cleanly.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder ExitTerminal(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        return builder
            .Type("exit")
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Applies the built sequence to the terminal, handling exceptions and asserting on failures.
    /// This method catches <see cref="TimeoutException"/>,
    /// logs the terminal content, and fails the test with a descriptive message.
    /// </summary>
    /// <param name="sequence">The built input sequence to apply.</param>
    /// <param name="terminal">The terminal to apply the sequence to.</param>
    /// <param name="output">Optional test output helper for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyWithAssertionsAsync(
        this Hex1bTerminalInputSequence sequence,
        Hex1bTerminal terminal,
        ITestOutputHelper? output = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await sequence.ApplyAsync(terminal, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            output?.WriteLine($"Operation timed out: {ex.Message}");

            // Grab a final snapshot to include in the failure message
            using var snapshot = terminal.CreateSnapshot();
            var content = snapshot.GetScreenText();
            output?.WriteLine("Final terminal content:");
            output?.WriteLine(content);

            Assert.Fail($"Test timed out. Terminal content:\n{content}");
        }
    }
}
