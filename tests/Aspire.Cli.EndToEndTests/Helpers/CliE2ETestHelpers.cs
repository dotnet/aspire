// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Terminal;
#pragma warning disable IDE0005 // Incorrectly flagged as unused due to types spread across namespaces
using Hex1b.Terminal.Automation;
#pragma warning restore IDE0005
using Xunit;

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
    /// Shell to use. Defaults to pwsh on Windows, /bin/bash on Linux/macOS.
    /// </summary>
    public string Shell { get; init; } = OperatingSystem.IsWindows() ? "pwsh" : "/bin/bash";

    /// <summary>
    /// Shell arguments. Defaults to ["-NoProfile", "-NoLogo"] on Windows,
    /// ["--norc", "--noprofile"] on Linux/macOS for a clean environment.
    /// </summary>
    public string[] ShellArgs { get; init; } = OperatingSystem.IsWindows()
        ? ["-NoProfile", "-NoLogo"]
        : ["--norc", "--noprofile"];

    /// <summary>
    /// Whether to inherit environment variables from the parent process.
    /// </summary>
    public bool InheritEnvironment { get; init; } = true;
}

/// <summary>
/// Helper methods for creating and managing Hex1b terminal sessions for Aspire CLI testing.
/// </summary>
public static class CliE2ETestHelpers
{
    /// <summary>
    /// Gets whether the tests are running in CI (GitHub Actions) vs locally.
    /// When running locally, some commands are replaced with echo stubs.
    /// </summary>
    public static bool IsRunningInCI =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_SHA"));

    /// <summary>
    /// Gets the PR number from the GITHUB_PR_NUMBER environment variable.
    /// When running locally (not in CI), returns a dummy value (0) for testing.
    /// </summary>
    /// <returns>The PR number, or 0 when running locally.</returns>
    public static int GetRequiredPrNumber()
    {
        var prNumberStr = Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER");

        if (string.IsNullOrEmpty(prNumberStr))
        {
            // Running locally - return dummy value
            return 0;
        }

        Assert.True(int.TryParse(prNumberStr, out var prNumber), $"GITHUB_PR_NUMBER must be a valid integer, got: {prNumberStr}");
        return prNumber;
    }

    /// <summary>
    /// Gets the commit SHA from the GITHUB_SHA environment variable.
    /// When running locally (not in CI), returns a dummy value for testing.
    /// </summary>
    /// <returns>The commit SHA, or a dummy value when running locally.</returns>
    public static string GetRequiredCommitSha()
    {
        var commitSha = Environment.GetEnvironmentVariable("GITHUB_SHA");

        if (string.IsNullOrEmpty(commitSha))
        {
            // Running locally - return dummy value
            return "local0000";
        }

        return commitSha;
    }

    /// <summary>
    /// Gets the path for storing asciinema recordings that will be uploaded as CI artifacts.
    /// In CI, this returns a path under $GITHUB_WORKSPACE/testresults/recordings/.
    /// Locally, this returns a path under the system temp directory.
    /// </summary>
    /// <param name="testName">The name of the test (used as the recording filename).</param>
    /// <returns>The full path to the .cast recording file.</returns>
    public static string GetTestResultsRecordingPath(string testName)
    {
        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        string recordingsDir;

        if (!string.IsNullOrEmpty(githubWorkspace))
        {
            // CI environment - write directly to test results for artifact upload
            recordingsDir = Path.Combine(githubWorkspace, "testresults", "recordings");
        }
        else
        {
            // Local development - use temp directory
            recordingsDir = Path.Combine(Path.GetTempPath(), "aspire-cli-e2e", "recordings");
        }

        Directory.CreateDirectory(recordingsDir);
        return Path.Combine(recordingsDir, $"{testName}.cast");
    }

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
