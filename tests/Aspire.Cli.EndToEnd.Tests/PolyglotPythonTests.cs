// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI with Python polyglot AppHost.
/// Tests creating a Python apphost and adding integrations.
/// Note: Does not run the apphost since Python runtime may not be available on CI.
/// </summary>
public sealed class PolyglotPythonTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreatePythonAppHostWithRedis()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreatePythonAppHostWithRedis));

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

        // Exit the shell
        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;

        // Verify generated files contain expected code
        var apphostFile = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.py");
        Assert.True(File.Exists(apphostFile), "apphost.py should exist");

        var apphostContent = await File.ReadAllTextAsync(apphostFile);
        Assert.Contains("from aspire import create_builder", apphostContent);
        Assert.Contains("builder = create_builder()", apphostContent);
        Assert.Contains("builder.build().run()", apphostContent);

        // Verify the generated SDK contains the add_redis method after adding Redis integration
        var aspireModuleFile = Path.Combine(workspace.WorkspaceRoot.FullName, ".modules", "aspire.py");
        Assert.True(File.Exists(aspireModuleFile), ".modules/aspire.py should exist after adding integration");

        var aspireModuleContent = await File.ReadAllTextAsync(aspireModuleFile);
        Assert.Contains("def add_redis(", aspireModuleContent);
    }
}
