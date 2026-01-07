// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI acquisition (download and installation).
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class AcquisitionTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _workDirectory;

    public AcquisitionTests(ITestOutputHelper output)
    {
        _output = output;

        // Create a unique work directory for this test run
        _workDirectory = Path.Combine(Path.GetTempPath(), "aspire-cli-e2e", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_workDirectory);

        _output.WriteLine($"Work directory: {_workDirectory}");
    }

    [Fact]
    public async Task DownloadAndVerifyAspireCliFromPR()
    {
        // Get PR number and commit SHA from environment variables (set by CI in run-tests.yml)
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();

        _output.WriteLine($"Testing CLI from PR #{prNumber} (commit: {commitSha})");

        // Create automation builder (handles recording path automatically)
        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "acquisition-test",
            output: _output,
            prNumber: prNumber);

        // Run the CLI acquisition and verification sequence
        _output.WriteLine($"Downloading and installing Aspire CLI from PR #{prNumber}...");
        _output.WriteLine($"Will verify version contains commit SHA: {commitSha}");

        await builder
            .PrepareEnvironment()
            .DownloadAndInstallAspireCli(prNumber)
            .SourceAspireCliEnvironment()
            .VerifyAspireCliVersion(commitSha)
            .ExitTerminal()
            .ExecuteAsync();
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
