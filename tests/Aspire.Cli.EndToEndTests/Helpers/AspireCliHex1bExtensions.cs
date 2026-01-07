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
    /// Prepares the shell environment with a custom prompt that tracks command count and exit status.
    /// This makes it easier to detect when commands complete. The prompt format is:
    /// [N ✔] $ (success) or [N ✘:code] $ (failure)
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder PrepareEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        // Set up a prompt that shows command count and exit status
        // This makes it easy to detect when a command has completed
        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo ✔ || echo ✘:$s)] \\$ \"'";

        return builder
            .Type(promptSetup)
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Waits for a specific command sequence number to appear in the prompt.
    /// The prompt format is [N ✔] $ (success) or [N ✘:code] $ (failure).
    /// If the command succeeded (✔), returns normally. If it failed (✘), throws an exception.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="sequenceNumber">The command sequence number to wait for.</param>
    /// <param name="timeout">Maximum time to wait (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="TerminalCommandFailedException">Thrown if the command failed (prompt shows ✘).</exception>
    public static Hex1bTerminalInputSequenceBuilder WaitForSequence(
        this Hex1bTerminalInputSequenceBuilder builder,
        int sequenceNumber,
        TimeSpan? timeout = null)
    {
        var successPattern = $"[{sequenceNumber} ✔]";
        var failurePattern = $"[{sequenceNumber} ✘";

        return builder.WaitUntil(
            snapshot =>
            {
                var text = snapshot.GetScreenText();

                // Check for failure first
                if (text.Contains(failurePattern, StringComparison.Ordinal))
                {
                    throw new TerminalCommandFailedException(
                        $"Command {sequenceNumber} failed.",
                        snapshot,
                        sequenceNumber);
                }

                // Check for success
                return text.Contains(successPattern, StringComparison.Ordinal);
            },
            timeout ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Downloads and installs the Aspire CLI for a specific PR number using the official PR download script.
    /// Waits for the installation to complete (up to 5 minutes by default).
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="prNumber">The PR number to download.</param>
    /// <param name="timeout">Maximum time to wait for installation (default: 5 minutes).</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder DownloadAndInstallAspireCli(
        this Hex1bTerminalInputSequenceBuilder builder,
        int prNumber,
        TimeSpan? timeout = null)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";

        return builder
            .Type(command)
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains("Successfully added aspire to", StringComparison.OrdinalIgnoreCase),
                timeout ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Sources the Aspire CLI environment to make the 'aspire' command available in the current shell.
    /// On Linux, this sources ~/.bashrc which contains the PATH updates from the installer.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder SourceAspireCliEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        // The installer adds aspire to ~/.dotnet/tools and updates ~/.bashrc
        // We need to source ~/.bashrc to pick up the PATH changes
        return builder
            .Type("source ~/.bashrc")
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Verifies the Aspire CLI installation by running 'aspire --version' and waiting for the expected commit SHA.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="expectedCommitSha">The commit SHA to look for in the version output.</param>
    /// <param name="timeout">Maximum time to wait for version output (default: 30 seconds).</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder VerifyAspireCliVersion(
        this Hex1bTerminalInputSequenceBuilder builder,
        string expectedCommitSha,
        TimeSpan? timeout = null)
    {
        return builder
            .Type("aspire --version")
            .Enter()
            .WaitUntil(
                snapshot => snapshot.GetScreenText().Contains(expectedCommitSha, StringComparison.OrdinalIgnoreCase),
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
    /// This method catches <see cref="TerminalCommandFailedException"/> and <see cref="TimeoutException"/>,
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
        catch (TerminalCommandFailedException ex)
        {
            output?.WriteLine($"Command {ex.CommandSequence} failed.");
            output?.WriteLine("Terminal content:");
            output?.WriteLine(ex.TerminalContent);

            Assert.Fail($"Command {ex.CommandSequence} failed. Terminal content:\n{ex.TerminalContent}");
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
