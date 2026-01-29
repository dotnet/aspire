// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI run command (creating and launching projects).
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class SmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateAndRunAspireStarterProject()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateAndRunAspireStarterProject));
        
        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

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

        var waitForCtrlCMessage = new CellPatternSearcher()
            .Find($"Press CTRL+C to stop the apphost and exit.");

        // Regression test for https://github.com/dotnet/aspire/issues/13971
        // If this prompt appears, it means multiple apphosts were incorrectly detected
        // (e.g., AppHost.cs was incorrectly treated as a single-file apphost)
        var unexpectedAppHostSelectionPrompt = new CellPatternSearcher()
            .Find("Select an apphost to use:");
        
        // The purpose of this is to keep track of the number of actual shell commands we have
        // executed. This is important because we customize the shell prompt to show either
        // "[n OK] $ " or "[n ERR:exitcode] $ ". This allows us to deterministically wait for a
        // command to complete and for the shell to be ready for more input rather than relying
        // on arbitrary timeouts of mid-command strings. We pass the counter into places where
        // we need to wait for command completion and use the value of the counter to detect
        // the command sequence output. We cannot hard code this value for each WaitForSuccessPrompt
        // call because depending on whether we are running CI or locally we might want to change
        // the commands we run and hence the sequence numbers. The commands we run can also
        // vary by platform, for example on Windows we can skip sourcing the environment the
        // way we do on Linux/macOS.
        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

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
            .WaitForSuccessPrompt(counter)
            .Type("aspire run")
            .Enter()
            .WaitUntil(s =>
            {
                // Fail immediately if we see the apphost selection prompt (means duplicate detection)
                if (unexpectedAppHostSelectionPrompt.Search(s).Count > 0)
                {
                    throw new InvalidOperationException(
                        "Unexpected apphost selection prompt detected! " +
                        "This indicates multiple apphosts were incorrectly detected.");
                }
                return waitForCtrlCMessage.Search(s).Count > 0;
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
