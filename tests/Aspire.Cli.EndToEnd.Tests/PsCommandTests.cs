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
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for aspire new prompts
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .Find("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./AspirePsTestApp): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find($"Do you want to create a test project?");

        var waitForProjectCreatedSuccessfullyMessage = new CellPatternSearcher()
            .Find("Project created successfully.");

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

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create a new project using aspire new
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select first template (Starter App)
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("AspirePsTestApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspirePsTestApp/AspirePsTestApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // First, verify aspire ps shows no running AppHosts
        sequenceBuilder.Type("aspire ps")
            .Enter()
            .WaitUntil(s => waitForNoRunningAppHosts.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
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
}
