// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Hex1b.Terminal.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI acquisition (download and installation).
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class AcquisitionTests : IAsyncDisposable
{
    private static readonly string[] s_installSuccessPatterns =
    [
        "Aspire CLI installed successfully",
        "aspire is already installed",
        "Installation complete"
    ];

    private static readonly string[] s_installErrorPatterns =
    [
        "Error:",
        "Failed to",
        "curl: ",
        "command not found",
        "No such file or directory"
    ];

    private static readonly string[] s_versionSuccessPatterns =
    [
        "aspire version"  // Output format: "aspire version X.Y.Z+hash"
    ];

    private static readonly string[] s_versionErrorPatterns =
    [
        "command not found",
        "not recognized",
        "No such file"
    ];

    private readonly ITestOutputHelper _output;
    private readonly string _workDirectory;
    private readonly string _recordingsDirectory;

    public AcquisitionTests(ITestOutputHelper output)
    {
        _output = output;

        // Create a unique work directory for this test run
        _workDirectory = Path.Combine(Path.GetTempPath(), "aspire-cli-e2e", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_workDirectory);

        // Create recordings directory for asciinema recordings
        _recordingsDirectory = Path.Combine(_workDirectory, "recordings");
        Directory.CreateDirectory(_recordingsDirectory);

        _output.WriteLine($"Work directory: {_workDirectory}");
        _output.WriteLine($"Recordings directory: {_recordingsDirectory}");
    }

    [Fact]
    public async Task DownloadAndVerifyAspireCliFromPR()
    {
        // Skip on non-Linux/macOS platforms since Hex1b PTY requires them
        if (!AspireHex1bHelpers.IsPlatformSupported())
        {
            _output.WriteLine($"Skipping test: {AspireHex1bHelpers.GetPlatformSkipReason()}");
            return;
        }

        // Get PR number from environment variable (set by CI)
        // The get-aspire-cli-pr.sh script requires a valid PR number - it doesn't support main branch
        var prNumberStr = Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER")
            ?? Environment.GetEnvironmentVariable("PR_NUMBER");

        if (string.IsNullOrEmpty(prNumberStr) || !int.TryParse(prNumberStr, out var prNumber))
        {
            _output.WriteLine("Skipping test: No PR number available. Set GITHUB_PR_NUMBER or PR_NUMBER environment variable.");
            _output.WriteLine("The get-aspire-cli-pr.sh script requires a valid PR number to download CLI artifacts.");
            return;
        }

        _output.WriteLine($"Testing CLI from PR #{prNumber}");

        // Set up the asciinema recording file
        var castFile = Path.Combine(_recordingsDirectory, "acquisition-test.cast");

        // Create terminal session using helper
        await using var session = await AspireHex1bHelpers.CreateAcquisitionTestSessionAsync(
            workingDirectory: _workDirectory,
            recordingPath: castFile,
            prNumber);

        var terminal = session.Terminal;
        var process = session.Process;
        var recorder = session.Recorder;

        _output.WriteLine("Terminal started, beginning automation sequence...");

        try
        {
            // Wait for shell to initialize
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Step 1: Set up PATH for aspire CLI
            _output.WriteLine("Setting up PATH...");
            var pathSequence = new Hex1bTerminalInputSequenceBuilder()
                .SourceAspireCliEnvironment()
                .Build();
            await pathSequence.ApplyAsync(terminal);

            // Step 2: Download and install Aspire CLI
            _output.WriteLine($"Downloading and installing Aspire CLI from PR #{prNumber}...");
            var installSequence = new Hex1bTerminalInputSequenceBuilder()
                .DownloadAndInstallAspireCli(prNumber)
                .Build();
            await installSequence.ApplyAsync(terminal);

            // Wait for installation to complete (up to 10 minutes)
            var installResult = await terminal.WaitUntilAsync(
                s_installSuccessPatterns,
                s_installErrorPatterns,
                timeout: TimeSpan.FromMinutes(10),
                pollInterval: TimeSpan.FromSeconds(2));

            if (installResult.IsError)
            {
                _output.WriteLine($"Installation failed with error pattern: {installResult.MatchedPattern}");
                _output.WriteLine("Terminal content:");
                _output.WriteLine(installResult.TerminalContent);
                Assert.Fail($"CLI installation failed: {installResult.MatchedPattern}");
            }

            if (!installResult.Success)
            {
                _output.WriteLine("Installation timed out. Terminal content:");
                _output.WriteLine(installResult.TerminalContent);
                Assert.Fail("CLI installation timed out after 10 minutes");
            }

            _output.WriteLine($"Installation succeeded, matched: {installResult.MatchedPattern}");

            // Step 3: Verify CLI is installed by checking version
            _output.WriteLine("Verifying Aspire CLI installation...");
            var verifySequence = new Hex1bTerminalInputSequenceBuilder()
                .VerifyAspireCliInstalled()
                .Build();
            await verifySequence.ApplyAsync(terminal);

            // Wait for version output (up to 30 seconds)
            var versionResult = await terminal.WaitUntilAsync(
                s_versionSuccessPatterns,
                s_versionErrorPatterns,
                timeout: TimeSpan.FromSeconds(30),
                pollInterval: TimeSpan.FromMilliseconds(500));

            if (versionResult.IsError)
            {
                _output.WriteLine($"Version check failed with error pattern: {versionResult.MatchedPattern}");
                _output.WriteLine("Terminal content:");
                _output.WriteLine(versionResult.TerminalContent);
                Assert.Fail($"CLI version check failed: {versionResult.MatchedPattern}");
            }

            if (!versionResult.Success)
            {
                _output.WriteLine("Version check timed out. Terminal content:");
                _output.WriteLine(versionResult.TerminalContent);
                Assert.Fail("CLI version check timed out");
            }

            _output.WriteLine($"Version check succeeded, matched: {versionResult.MatchedPattern}");
            _output.WriteLine("Terminal content:");
            _output.WriteLine(versionResult.TerminalContent);

            // Step 4: Exit the terminal
            _output.WriteLine("Exiting terminal...");
            var exitSequence = new Hex1bTerminalInputSequenceBuilder()
                .ExitTerminal()
                .Build();
            await exitSequence.ApplyAsync(terminal);

            // Wait for the process to exit
            var exitCode = await AspireHex1bHelpers.WaitForExitAsync(process, TimeSpan.FromSeconds(10));
            _output.WriteLine($"Terminal process exited with code: {exitCode?.ToString() ?? "killed"}");
        }
        catch (OperationCanceledException)
        {
            _output.WriteLine("Test timed out, killing process...");
            process.Kill();
            throw;
        }
        finally
        {
            // Flush the recorder to ensure all data is written
            if (recorder is not null)
            {
                await recorder.FlushAsync();
                _output.WriteLine($"Asciinema recording saved to: {castFile}");
            }

            // Copy recording to test results location if available
            CopyRecordingsToTestResults();
        }

        // Verify the recording was created
        Assert.True(File.Exists(castFile), $"Expected asciinema recording at {castFile}");

        var castContent = await File.ReadAllTextAsync(castFile);
        Assert.NotEmpty(castContent);
        _output.WriteLine($"Recording size: {castContent.Length} bytes");
    }

    private void CopyRecordingsToTestResults()
    {
        // CI artifacts upload paths from run-tests.yml:
        // - testresults/** (always uploaded)
        // - artifacts/log/test-logs/** (uploaded when TEST_LOG_PATH is set)
        //
        // We prefer TEST_LOG_PATH if available, otherwise use the testresults directory
        // which is always uploaded by the CI workflow.

        var testLogPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
        var testResultsPath = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") is { } workspace
            ? Path.Combine(workspace, "testresults", "recordings")
            : null;

        // Try TEST_LOG_PATH first, then fall back to testresults directory
        var targetDir = !string.IsNullOrEmpty(testLogPath)
            ? Path.Combine(testLogPath, "recordings")
            : testResultsPath;

        if (string.IsNullOrEmpty(targetDir))
        {
            _output.WriteLine("No CI artifact path available - recordings will not be uploaded");
            return;
        }

        try
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(_recordingsDirectory, "*.cast"))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, overwrite: true);
                _output.WriteLine($"Copied recording to: {targetFile}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Failed to copy recordings to test results: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up work directory
        try
        {
            if (Directory.Exists(_workDirectory))
            {
                Directory.Delete(_workDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Failed to clean up work directory: {ex.Message}");
        }

        await ValueTask.CompletedTask;
    }
}
