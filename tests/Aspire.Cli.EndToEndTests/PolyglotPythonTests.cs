// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI with Python polyglot AppHost.
/// Tests creating a Python apphost, adding Redis integration, and running it.
/// </summary>
public sealed class PolyglotPythonTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreatePythonAppHostWithRedisAndRun()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreatePythonAppHostWithRedisAndRun));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect successful apphost creation
        var waitForAppHostCreated = new CellPatternSearcher()
            .Find("Created apphost.py");

        // Pattern to detect Redis integration added
        var waitForRedisAdded = new CellPatternSearcher()
            .Find("Added Aspire.Hosting.Redis");

        // Pattern to detect dashboard is ready (Ctrl+C message)
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

        // Step 1: Create Python apphost
        sequenceBuilder
            .Type("aspire init -l python")
            .Enter()
            .WaitUntil(s => waitForAppHostCreated.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        // Step 2: Add Redis integration
        sequenceBuilder
            .Type("aspire add redis")
            .Enter()
            .WaitUntil(s => waitForRedisAdded.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        // Step 3: Run the apphost and wait for dashboard
        sequenceBuilder
            .Type("aspire run")
            .Enter()
            .WaitUntil(s => waitForCtrlCMessage.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .Ctrl().Key(Hex1b.Input.Hex1bKey.C)
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
