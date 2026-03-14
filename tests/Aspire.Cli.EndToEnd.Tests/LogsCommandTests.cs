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

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

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
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create a new project using aspire new
        await auto.AspireNewAsync("AspireLogsTestApp", counter);

        // Navigate to the AppHost directory
        await auto.TypeAsync("cd AspireLogsTestApp/AspireLogsTestApp.AppHost");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Start the AppHost in the background using aspire start
        await auto.TypeAsync("aspire start");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(3), description: "wait for AppHost started successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Wait for resources to fully start and produce logs
        await auto.TypeAsync("sleep 15");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Test aspire logs for a specific resource (apiservice) - non-follow mode gets logs and exits
        await auto.TypeAsync("aspire logs apiservice > logs.txt 2>&1");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Debug: show file size and first few lines
        await auto.TypeAsync("wc -l logs.txt && head -5 logs.txt");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify the log file contains expected output
        await auto.TypeAsync("cat logs.txt | grep -E '\\[apiservice\\]' | head -3");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitForApiserviceLogs.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(10), description: "wait for apiservice log lines");
        await auto.WaitForSuccessPromptAsync(counter);

        // Test aspire logs --format json for a specific resource
        await auto.TypeAsync("aspire logs apiservice --format json > logs_json.txt 2>&1");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Verify the JSON log file contains expected output
        await auto.TypeAsync("cat logs_json.txt | grep '\"resourceName\"' | head -3");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitForLogsJsonOutput.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(10), description: "wait for JSON log output");
        await auto.WaitForSuccessPromptAsync(counter);

        // Stop the AppHost using aspire stop
        await auto.TypeAsync("aspire stop");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(1), description: "wait for AppHost stopped successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
