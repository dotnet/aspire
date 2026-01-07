// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal;
#pragma warning disable IDE0005 // Incorrectly flagged as unused due to types spread across namespaces
using Hex1b.Terminal.Automation;
#pragma warning restore IDE0005

namespace Aspire.Cli.EndToEndTests.Helpers;

/// <summary>
/// Result tuple from creating a Hex1b terminal session for Aspire CLI testing.
/// </summary>
/// <param name="Terminal">The Hex1b terminal instance.</param>
/// <param name="Presentation">The headless presentation adapter.</param>
/// <param name="Process">The child process workload adapter.</param>
/// <param name="Recorder">The asciinema recorder (if recording is enabled).</param>
public sealed record AspireTerminalSession(
    Hex1bTerminal Terminal,
    HeadlessPresentationAdapter Presentation,
    Hex1bTerminalChildProcess Process,
    AsciinemaRecorder? Recorder) : IAsyncDisposable
{
    /// <summary>
    /// Disposes all resources in the correct order.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Recorder is not null)
        {
            await Recorder.FlushAsync();
        }

        Terminal.Dispose();
        Presentation.Dispose();
        await Process.DisposeAsync();
    }
}

/// <summary>
/// Options for creating an Aspire CLI terminal session.
/// </summary>
public sealed class AspireTerminalOptions
{
    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    public int Width { get; init; } = 120;

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    public int Height { get; init; } = 40;

    /// <summary>
    /// Working directory for the terminal session.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Path to save the asciinema recording. If null, recording is disabled.
    /// </summary>
    public string? RecordingPath { get; init; }

    /// <summary>
    /// Title for the asciinema recording.
    /// </summary>
    public string? RecordingTitle { get; init; }

    /// <summary>
    /// Whether to capture input in the asciinema recording.
    /// </summary>
    public bool CaptureInput { get; init; } = true;

    /// <summary>
    /// Shell to use. Defaults to /bin/bash.
    /// </summary>
    public string Shell { get; init; } = "/bin/bash";

    /// <summary>
    /// Shell arguments. Defaults to ["--norc", "--noprofile"] for a clean environment.
    /// </summary>
    public string[] ShellArgs { get; init; } = ["--norc", "--noprofile"];

    /// <summary>
    /// Whether to inherit environment variables from the parent process.
    /// </summary>
    public bool InheritEnvironment { get; init; } = true;
}

/// <summary>
/// Helper methods for creating and managing Hex1b terminal sessions for Aspire CLI testing.
/// </summary>
public static class AspireHex1bHelpers
{
    /// <summary>
    /// Creates a new terminal session with all components configured for Aspire CLI testing.
    /// </summary>
    /// <param name="options">Options for configuring the terminal session.</param>
    /// <returns>A tuple containing the terminal, presentation adapter, process, and optional recorder.</returns>
    public static async Task<AspireTerminalSession> CreateTerminalSessionAsync(AspireTerminalOptions? options = null)
    {
        options ??= new AspireTerminalOptions();

        // Create the headless presentation adapter
        var presentation = new HeadlessPresentationAdapter(options.Width, options.Height);

        // Create the child process
        var process = new Hex1bTerminalChildProcess(
            options.Shell,
            options.ShellArgs,
            workingDirectory: options.WorkingDirectory,
            inheritEnvironment: options.InheritEnvironment,
            initialWidth: options.Width,
            initialHeight: options.Height
        );

        // Build terminal options
        var terminalOptions = new Hex1bTerminalOptions
        {
            Width = options.Width,
            Height = options.Height,
            PresentationAdapter = presentation,
            WorkloadAdapter = process
        };

        // Add asciinema recorder if recording path is specified
        AsciinemaRecorder? recorder = null;
        if (!string.IsNullOrEmpty(options.RecordingPath))
        {
            recorder = terminalOptions.AddAsciinemaRecorder(options.RecordingPath, new AsciinemaRecorderOptions
            {
                Title = options.RecordingTitle ?? "Aspire CLI Test",
                CaptureInput = options.CaptureInput
            });
        }

        // Create the terminal
        var terminal = new Hex1bTerminal(terminalOptions);

        // Start the process
        await process.StartAsync();

        return new AspireTerminalSession(terminal, presentation, process, recorder);
    }

    /// <summary>
    /// Creates a new terminal session with default settings for Aspire CLI acquisition testing.
    /// </summary>
    /// <param name="workingDirectory">Working directory for the terminal.</param>
    /// <param name="recordingPath">Path to save the asciinema recording.</param>
    /// <param name="prNumber">Optional PR number for the recording title.</param>
    /// <returns>A configured terminal session.</returns>
    public static Task<AspireTerminalSession> CreateAcquisitionTestSessionAsync(
        string workingDirectory,
        string? recordingPath = null,
        int? prNumber = null)
    {
        var title = prNumber.HasValue
            ? $"Aspire CLI Acquisition (PR #{prNumber})"
            : "Aspire CLI Acquisition (main)";

        return CreateTerminalSessionAsync(new AspireTerminalOptions
        {
            WorkingDirectory = workingDirectory,
            RecordingPath = recordingPath,
            RecordingTitle = title
        });
    }

    /// <summary>
    /// Checks if the current platform supports Hex1b PTY operations.
    /// </summary>
    /// <returns>True if the platform is supported (Linux or macOS).</returns>
    public static bool IsPlatformSupported()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    }

    /// <summary>
    /// Gets the skip reason message for unsupported platforms.
    /// </summary>
    /// <returns>A message explaining why the test was skipped.</returns>
    public static string GetPlatformSkipReason()
    {
        return "Hex1b PTY requires Linux or macOS.";
    }

    /// <summary>
    /// Waits for the terminal to show a shell prompt.
    /// </summary>
    /// <param name="terminal">The terminal to wait on.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a prompt was detected, false if timed out.</returns>
    public static async Task<bool> WaitForShellPromptAsync(
        Hex1bTerminal terminal,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var snapshot = terminal.CreateSnapshot();
            var content = snapshot.GetScreenText();

            // Common shell prompt indicators
            if (content.Contains('$') || content.Contains('#') || content.Contains('>'))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Gets the current terminal screen content.
    /// </summary>
    /// <param name="terminal">The terminal to read from.</param>
    /// <returns>The current screen text.</returns>
    public static string GetScreenContent(Hex1bTerminal terminal)
    {
        using var snapshot = terminal.CreateSnapshot();
        return snapshot.GetScreenText();
    }

    /// <summary>
    /// Waits for the terminal process to exit with a timeout.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code, or null if the process was killed due to timeout.</returns>
    public static async Task<int?> WaitForExitAsync(
        Hex1bTerminalChildProcess process,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(10);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout.Value);

        try
        {
            var exitCode = await process.WaitForExitAsync(cts.Token);
            return exitCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout - kill the process
            process.Kill();
            return null;
        }
    }
}
