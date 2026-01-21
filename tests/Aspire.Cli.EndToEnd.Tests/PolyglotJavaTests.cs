// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI with Java polyglot AppHost.
/// Tests creating a Java apphost and adding integrations.
/// Note: Does not run the apphost since Java runtime may not be available on CI.
/// </summary>
public sealed class PolyglotJavaTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateJavaAppHostWithRedis()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateJavaAppHostWithRedis));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect successful apphost creation
        var waitForAppHostCreated = new CellPatternSearcher()
            .Find("Created AppHost.java");

        // Pattern to detect Redis integration added
        var waitForRedisAdded = new CellPatternSearcher()
            .Find("The package Aspire.Hosting.Redis::");

        // In CI, aspire add shows a version selection prompt
        var waitingForAddVersionSelectionPrompt = new CellPatternSearcher()
            .Find("Select a version of Aspire.Hosting.Redis");

        // Pattern to confirm PR version is selected
        var waitingForPrVersionSelected = new CellPatternSearcher()
            .Find($"> pr-{prNumber}");

        // Pattern to confirm specific version with short SHA is selected (e.g., "> 9.3.0-dev.g1234567")
        var shortSha = commitSha[..7]; // First 7 characters of commit SHA
        var waitingForShaVersionSelected = new CellPatternSearcher()
            .Find($"g{shortSha}");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Enable polyglot support feature flag
        sequenceBuilder.EnablePolyglotSupport(counter);

        // Step 1: Create Java apphost
        sequenceBuilder
            .Type("aspire init -l java")
            .Enter()
            .WaitUntil(s => waitForAppHostCreated.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .WaitForSuccessPrompt(counter);

        // Step 2: Add Redis integration
        sequenceBuilder
            .Type("aspire add redis")
            .Enter();

        // In CI, aspire add shows a version selection prompt (unlike aspire new which auto-selects when channel is set)
        if (isCI)
        {
            // First prompt: Select the PR channel (pr-XXXXX)
            sequenceBuilder
                .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                // Navigate down to the PR channel option
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .WaitUntil(s => waitingForPrVersionSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                .Enter() // select PR channel
                .WaitUntil(s => waitingForShaVersionSelected.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter(); // select specific version
        }

        sequenceBuilder
            .WaitUntil(s => waitForRedisAdded.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .WaitForSuccessPrompt(counter);

        // Exit the shell
        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;

        // Verify generated files contain expected code
        var apphostFile = Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.java");
        Assert.True(File.Exists(apphostFile), "AppHost.java should exist");

        var apphostContent = await File.ReadAllTextAsync(apphostFile);
        Assert.Contains("package aspire;", apphostContent);
        Assert.Contains("Aspire.createBuilder(", apphostContent);
        Assert.Contains("builder.build()", apphostContent);

        // Verify the generated SDK contains the addRedis method after adding Redis integration
        var aspireModuleFile = Path.Combine(workspace.WorkspaceRoot.FullName, ".modules", "Aspire.java");
        Assert.True(File.Exists(aspireModuleFile), ".modules/Aspire.java should exist after adding integration");

        var aspireModuleContent = await File.ReadAllTextAsync(aspireModuleFile);
        Assert.Contains("static IDistributedApplicationBuilder createBuilder(", aspireModuleContent);
        Assert.Contains("IRedisResource addRedis(", aspireModuleContent);

        // Verify settings.json was created with the Redis package
        var settingsFile = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json");
        Assert.True(File.Exists(settingsFile), ".aspire/settings.json should exist after adding integration");

        var settingsContent = await File.ReadAllTextAsync(settingsFile);
        Assert.Contains("Aspire.Hosting.Redis", settingsContent);
    }
}
