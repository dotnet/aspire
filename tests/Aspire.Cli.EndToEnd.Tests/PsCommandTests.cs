// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for the aspire ps command.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class PsCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PsCommandListsRunningAppHost()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/stop/ps commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        // Pattern for aspire ps output - should show the AppHost path and PID columns
        var waitForPsOutputWithAppHost = new CellPatternSearcher()
            .Find("AspirePsTestApp.AppHost");

        // Pattern for aspire ps JSON output
        var waitForPsJsonOutput = new CellPatternSearcher()
            .Find("\"appHostPath\":");

        // Pattern for aspire ps when no AppHosts running
        var waitForNoRunningAppHosts = new CellPatternSearcher()
            .Find("No running AppHost found");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Create a new project using aspire new
        sequenceBuilder.AspireNew("AspirePsTestApp", counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspirePsTestApp/AspirePsTestApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // First, verify aspire ps shows no running AppHosts
        sequenceBuilder.Type("aspire ps")
            .Enter()
            .WaitUntil(s => waitForNoRunningAppHosts.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire start
        sequenceBuilder.Type("aspire start")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Now verify aspire ps shows the running AppHost
        sequenceBuilder.Type("aspire ps")
            .Enter()
            .WaitUntil(s => waitForPsOutputWithAppHost.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Test aspire ps --format json output
        sequenceBuilder.Type("aspire ps --format json")
            .Enter()
            .WaitUntil(s => waitForPsJsonOutput.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Stop the AppHost using aspire stop
        sequenceBuilder.Type("aspire stop")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Verify aspire ps shows no running AppHosts again after stop
        sequenceBuilder.Type("aspire ps")
            .Enter()
            .WaitUntil(s => waitForNoRunningAppHosts.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task PsFormatJsonOutputsOnlyJsonToStdout()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        var outputFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "ps-output.json");
        var containerOutputFilePath = CliE2ETestHelpers.ToContainerPath(outputFilePath, workspace);

        // Run aspire ps --format json with stdout redirected to a file.
        // Status messages go to stderr (Spectre.Console spinner, cleared on completion),
        // JSON output goes to stdout (redirected to the file).
        // We only wait for the success prompt since the Spectre status spinner is
        // transient and erased before WaitUntil polling can observe it.
        sequenceBuilder.Type($"aspire ps --format json > {containerOutputFilePath}")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify the file contains only the expected JSON output (empty array).
        sequenceBuilder.ExecuteCallback(() =>
        {
            var content = File.ReadAllText(outputFilePath).Trim();
            Assert.Equal("[]", content);
        });

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
