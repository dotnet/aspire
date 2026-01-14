// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI with Python/React (FastAPI/Vite) template.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class PythonReactTemplateTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateAndRunPythonReactProject()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateAndRunPythonReactProject));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for template selection - we need to find and select "Starter App (FastAPI/React)"
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        // Wait for the FastAPI/React template to be highlighted (after pressing Down twice)
        // Use Find() instead of FindPattern() because parentheses and slashes are regex special characters
        var waitingForPythonReactTemplateSelected = new CellPatternSearcher()
            .Find("> Starter App (FastAPI/React)");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./AspirePyReactApp): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitForCtrlCMessage = new CellPatternSearcher()
            .Find($"Press CTRL+C to stop the apphost and exit.");

        // The purpose of this is to keep track of the number of actual shell commands we have
        // executed. This is important because we customize the shell prompt to show either
        // "[n OK] $ " or "[n ERR:exitcode] $ ". This allows us to deterministically wait for a
        // command to complete and for the shell to be ready for more input rather than relying
        // on arbitrary timeouts of mid-command strings.
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
            // Navigate down to "Starter App (FastAPI/React)" which is the 3rd option
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForPythonReactTemplateSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter() // select "Starter App (FastAPI/React)"
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("AspirePyReactApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default output path
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // select "No" for localhost URLs (default)
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            // For Redis prompt, default is "Yes" so we need to select "No" by pressing Down
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Enter() // select "No" for Redis Cache
            .WaitForSuccessPrompt(counter)
            .Type("aspire run")
            .Enter()
            .WaitUntil(s => waitForCtrlCMessage.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .Ctrl().Key(Hex1b.Input.Hex1bKey.C)
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
