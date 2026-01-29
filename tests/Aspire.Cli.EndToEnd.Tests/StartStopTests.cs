// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI start and stop commands (background/detached mode).
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class StartStopTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateStartAndStopAspireProject()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateStartAndStopAspireProject));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for aspire new prompts
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./AspireStarterApp): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find($"Do you want to create a test project?");

        var waitForProjectCreatedSuccessfullyMessage = new CellPatternSearcher()
            .Find("Project created successfully.");

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

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
            .Type("AspireStarterApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            // For URLs prompt, default is "Yes" so we need to select "No" by pressing Down
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Enter() // select "No" for localhost URLs
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default "Yes" for Redis Cache
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            // For test project prompt, default is "Yes" so we need to select "No" by pressing Down
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Enter() // select "No" for test project
            .WaitForSuccessPrompt(counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspireStarterApp/AspireStarterApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
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
