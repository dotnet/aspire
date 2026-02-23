// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI behavior when multiple AppHost projects are found.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class MultipleAppHostTests(ITestOutputHelper output)
{
    [Fact]
    public async Task MultipleAppHostsShowsSelectionPrompt()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(MultipleAppHostsShowsSelectionPrompt));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // aspire new prompts
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find("Enter the project name (");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find("Enter the output path:");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find("Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find("Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find("Do you want to create a test project?");

        // Pattern searcher for the apphost selection prompt
        var waitingForAppHostSelectionPrompt = new CellPatternSearcher()
            .Find("Select an apphost to use:");

        var waitForCtrlCMessage = new CellPatternSearcher()
            .Find("Press CTRL+C to stop the apphost and exit.");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create the first project using aspire new
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select Starter App
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("FirstApp")
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

        // Clear screen to avoid pattern interference from the first aspire new
        sequenceBuilder.ClearScreen(counter);

        // Create the second project using aspire new
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select Starter App
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("SecondApp")
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

        // Clear screen before running aspire run
        sequenceBuilder.ClearScreen(counter);

        // Searcher for error messages that indicate the apphost failed to build/resolve
        var sdkResolutionError = new CellPatternSearcher()
            .Find("Could not resolve SDK");

        var noAppHostFound = new CellPatternSearcher()
            .Find("No project file found");

        // Run aspire run from the workspace root — should find both apphosts and prompt
        sequenceBuilder.Type("aspire run")
            .Enter()
            .WaitUntil(s =>
            {
                // Assert we see the selection prompt
                if (waitingForAppHostSelectionPrompt.Search(s).Count > 0)
                {
                    return true;
                }

                // Fail fast with descriptive message if something went wrong instead
                if (sdkResolutionError.Search(s).Count > 0)
                {
                    throw new InvalidOperationException(
                        "AppHost SDK resolution failed. The test requires real buildable AppHost projects.");
                }

                if (noAppHostFound.Search(s).Count > 0)
                {
                    throw new InvalidOperationException(
                        "No AppHost projects were found. Expected two AppHost projects to trigger selection prompt.");
                }

                return false;
            }, TimeSpan.FromSeconds(60))
            // Select the first apphost by pressing Enter
            .Enter()
            // The selected apphost should build and start successfully
            .WaitUntil(s =>
            {
                if (waitForCtrlCMessage.Search(s).Count > 0)
                {
                    return true;
                }

                // Fail fast if the apphost failed to build
                if (sdkResolutionError.Search(s).Count > 0)
                {
                    throw new InvalidOperationException(
                        "Selected AppHost failed to build due to SDK resolution error.");
                }

                return false;
            }, TimeSpan.FromMinutes(2))
            .Ctrl().Key(Hex1b.Input.Hex1bKey.C)
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
