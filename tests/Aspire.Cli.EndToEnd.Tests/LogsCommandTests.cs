// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for the aspire logs command.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class LogsCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task LogsCommandShowsResourceLogs()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        // Pattern for verifying log output was written to file
        var waitForApiserviceLogs = new CellPatternSearcher()
            .Find("[apiservice]");

        // Pattern for verifying JSON log output was written to file
        var waitForLogsJsonOutput = new CellPatternSearcher()
            .Find("\"resourceName\":");

        // Pattern for aspire logs when no AppHosts running
        var waitForNoRunningAppHosts = new CellPatternSearcher()
            .Find("No running AppHost found");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Create a new project using aspire new
        sequenceBuilder.AspireNew("AspireLogsTestApp", counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspireLogsTestApp/AspireLogsTestApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Wait for resources to fully start and produce logs
        sequenceBuilder.Type("sleep 15")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Test aspire logs for a specific resource (apiservice) - non-follow mode gets logs and exits
        sequenceBuilder.Type("aspire logs apiservice > logs.txt 2>&1")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Debug: show file size and first few lines
        sequenceBuilder.Type("wc -l logs.txt && head -5 logs.txt")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify the log file contains expected output
        sequenceBuilder.Type("cat logs.txt | grep -E '\\[apiservice\\]' | head -3")
            .Enter()
            .WaitUntil(s => waitForApiserviceLogs.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Test aspire logs --format json for a specific resource
        sequenceBuilder.Type("aspire logs apiservice --format json > logs_json.txt 2>&1")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify the JSON log file contains expected output
        sequenceBuilder.Type("cat logs_json.txt | grep '\"resourceName\"' | head -3")
            .Enter()
            .WaitUntil(s => waitForLogsJsonOutput.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Stop the AppHost using aspire stop
        sequenceBuilder.Type("aspire stop")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
