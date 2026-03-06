// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Aspire.TestUtilities;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI wait command.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class WaitCommandTests(ITestOutputHelper output)
{
    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/14993")]
    public async Task CreateStartWaitAndStopAspireProject()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/wait/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForResourceUp = new CellPatternSearcher()
            .Find("is up (running).");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Create a new project using aspire new
        sequenceBuilder.AspireNew("AspireWaitApp", counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspireWaitApp/AspireWaitApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Wait for the webfrontend resource to be up (running).
        // Use a longer timeout in Docker-in-Docker where container startup is slower.
        sequenceBuilder.Type("aspire wait webfrontend --status up --timeout 300")
            .Enter()
            .WaitUntil(s => waitForResourceUp.Search(s).Count > 0, TimeSpan.FromMinutes(6))
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
