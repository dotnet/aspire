// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Smoke tests that run inside a Docker container using Hex1b's WithDockerContainer API.
/// These tests install the GA release of Aspire CLI from aspire.dev and verify it works
/// in an isolated container environment.
/// </summary>
public sealed class DockerSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task InstallAndVerifyAspireGAInDockerContainer()
    {
        _ = output; // Reserved for future logging use.

        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(InstallAndVerifyAspireGAInDockerContainer));

        using var terminal = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithDockerContainer()
            .Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // The Docker container runs as root, so the bash prompt uses '#' instead of '$'.
        var waitingForContainerReady = new CellPatternSearcher()
            .Find("# ");

        var waitingForAspireVersion = new CellPatternSearcher()
            .FindPattern(@"\d+\.\d+\.\d+");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        // Wait for the container to start and bash to show a prompt.
        sequenceBuilder
            .WaitUntil(s => waitingForContainerReady.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Wait(500);

        // Set up prompt tracking (same mechanism as PrepareEnvironment but without
        // changing to a host workspace directory that doesn't exist in the container).
        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";
        sequenceBuilder
            .Type(promptSetup)
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Install the GA release of Aspire CLI from aspire.dev.
        sequenceBuilder
            .Type("curl -sSL https://aspire.dev/install.sh | bash")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(120));

        // Add the Aspire CLI to PATH.
        sequenceBuilder
            .Type("export PATH=~/.aspire/bin:$PATH")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify the Aspire CLI is installed and reports a version.
        sequenceBuilder
            .Type("aspire --version")
            .Enter()
            .WaitUntil(s => waitingForAspireVersion.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
