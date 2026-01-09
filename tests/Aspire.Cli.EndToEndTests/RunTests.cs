// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI run command (creating and launching projects).
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class RunTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _workDirectory;

    public RunTests(ITestOutputHelper output)
    {
        _output = output;

        // Create a unique work directory for this test run
        _workDirectory = Path.Combine(Path.GetTempPath(), "aspire-cli-e2e", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_workDirectory);

        _output.WriteLine($"Work directory: {_workDirectory}");
    }

    [Fact]
    public async Task CreateAndRunAspireStarterProject()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing aspire-starter from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-starter",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspireStarterProject("StarterApp")
            .RunAspireProject("StarterApp")
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspireTypeScriptCSharpStarterProject()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing aspire-ts-cs-starter from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-ts-cs-starter",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspireTypeScriptCSharpStarterProject("TsCsApp")
            .RunAspireProject("TsCsApp")
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspirePythonStarterProject()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing aspire-py-starter from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-py-starter",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspirePythonStarterProject("PyApp")
            .RunAspireProject("PyApp", isFlatStructure: true)
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspireAppHostSingleFileProject()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing aspire-apphost-singlefile from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-apphost-singlefile",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspireAppHostSingleFileProject("SingleFileApp")
            .RunAspireProject("SingleFileApp", isFlatStructure: true)
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspireStarterProjectInteractively()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing interactive aspire-starter from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-starter-interactive",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspireStarterProjectInteractively("InteractiveStarterApp")
            .RunAspireProject("InteractiveStarterApp")
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspireTypeScriptCSharpStarterProjectInteractively()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing interactive aspire-ts-cs-starter from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-ts-cs-starter-interactive",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspireTypeScriptCSharpStarterProjectInteractively("InteractiveTsCsApp")
            .RunAspireProject("InteractiveTsCsApp")
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspirePythonStarterProjectInteractively()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing interactive aspire-py-starter from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-py-starter-interactive",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspirePythonStarterProjectInteractively("InteractivePyApp")
            .RunAspireProject("InteractivePyApp", isFlatStructure: true)
            .StopAspireProject()
            .ExitTerminal()
            .ExecuteAsync();
    }

    [Fact]
    public async Task CreateAndRunAspireAppHostSingleFileProjectInteractively()
    {
        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        _output.WriteLine($"Testing interactive aspire-apphost-singlefile from PR #{prNumber} (commit: {commitSha[..8]})");

        await using var builder = await AspireCliAutomationBuilder.CreateAsync(
            workingDirectory: _workDirectory,
            recordingName: "run-aspire-apphost-singlefile-interactive",
            output: _output,
            prNumber: prNumber);

        builder.PrepareEnvironment();

        if (CliE2ETestHelpers.IsRunningInCI)
        {
            builder
                .InstallAspireCliFromPullRequest(prNumber)
                .SourceAspireCliEnvironment()
                .VerifyAspireCliVersion(commitSha);
        }

        await builder
            .CreateAspireAppHostSingleFileProjectInteractively("InteractiveSingleFileApp")
            .RunAspireProject("InteractiveSingleFileApp", isFlatStructure: true)
            .StopAspireProject()
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
