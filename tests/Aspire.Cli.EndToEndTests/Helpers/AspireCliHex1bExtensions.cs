// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal;
using Hex1b.Terminal.Automation;

namespace Aspire.Cli.EndToEndTests.Helpers;

/// <summary>
/// Result of a WaitUntil operation.
/// </summary>
public sealed class WaitUntilResult
{
    public bool Success { get; init; }
    public string? MatchedPattern { get; init; }
    public bool IsError { get; init; }
    public string TerminalContent { get; init; } = string.Empty;
}

/// <summary>
/// Extension methods for Hex1b terminal automation to simplify Aspire CLI operations.
/// </summary>
internal static class AspireCliHex1bExtensions
{
    /// <summary>
    /// Downloads and installs the Aspire CLI for a specific PR number using the official PR download script.
    /// The script requires a valid PR number - it does not support downloading from main branch.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <param name="prNumber">The PR number to download (required).</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when prNumber is not provided.</exception>
    public static Hex1bTerminalInputSequenceBuilder DownloadAndInstallAspireCli(
        this Hex1bTerminalInputSequenceBuilder builder,
        int prNumber)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";

        return builder
            .Type(command)
            .Enter();
        // Note: Don't add Wait here - caller should use WaitUntilAsync to wait for completion
    }

    /// <summary>
    /// Verifies the Aspire CLI installation by running 'aspire --version'.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder VerifyAspireCliInstalled(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        return builder
            .Type("aspire --version")
            .Enter();
        // Note: Don't add Wait here - caller should use WaitUntilAsync to wait for output
    }

    /// <summary>
    /// Sources the Aspire CLI environment to make the 'aspire' command available in the current shell.
    /// </summary>
    /// <param name="builder">The input sequence builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static Hex1bTerminalInputSequenceBuilder SourceAspireCliEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder)
    {
        // The installer adds aspire to ~/.dotnet/tools, which should already be in PATH
        // But we may need to source the profile or rehash
        return builder
            .Type("export PATH=\"$HOME/.dotnet/tools:$PATH\"")
            .Enter()
            .Wait(TimeSpan.FromMilliseconds(500));
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
    /// Waits until the terminal content matches one of the success patterns or error patterns.
    /// Scans the terminal buffer periodically until a match is found or timeout is reached.
    /// </summary>
    /// <param name="terminal">The Hex1b terminal instance.</param>
    /// <param name="successPatterns">Patterns that indicate success.</param>
    /// <param name="errorPatterns">Patterns that indicate an error.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">How often to scan the terminal buffer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether success/error pattern was found.</returns>
    public static async Task<WaitUntilResult> WaitUntilAsync(
        this Hex1bTerminal terminal,
        string[] successPatterns,
        string[] errorPatterns,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(500);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // CreateSnapshot() auto-flushes pending output and gives us a consistent view
            using var snapshot = terminal.CreateSnapshot();
            var content = snapshot.GetScreenText();

            // Check for error patterns first
            foreach (var pattern in errorPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return new WaitUntilResult
                    {
                        Success = false,
                        IsError = true,
                        MatchedPattern = pattern,
                        TerminalContent = content
                    };
                }
            }

            // Check for success patterns
            foreach (var pattern in successPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return new WaitUntilResult
                    {
                        Success = true,
                        IsError = false,
                        MatchedPattern = pattern,
                        TerminalContent = content
                    };
                }
            }

            await Task.Delay(interval, cancellationToken);
        }

        // Timeout - return failure with current content
        using var finalSnapshot = terminal.CreateSnapshot();
        return new WaitUntilResult
        {
            Success = false,
            IsError = false,
            MatchedPattern = null,
            TerminalContent = finalSnapshot.GetScreenText()
        };
    }
}
