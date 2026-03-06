// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for aspire stop in non-interactive mode.
/// Validates fix for https://github.com/dotnet/aspire/issues/14558.
/// </summary>
public sealed class StopNonInteractiveTests(ITestOutputHelper output)
{
    [Fact]
    public async Task StopNonInteractiveSingleAppHost()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        var waitForNoRunningAppHostsFound = new CellPatternSearcher()
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
        sequenceBuilder.AspireNew("TestStopApp", counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd TestStopApp/TestStopApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Clear screen to avoid matching old patterns
        sequenceBuilder.ClearScreen(counter);

        // Stop the AppHost using aspire stop --non-interactive --project (targets specific AppHost)
        sequenceBuilder.Type("aspire stop --non-interactive --project TestStopApp.AppHost.csproj")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Verify that stop --non-interactive handles no running AppHosts gracefully
        sequenceBuilder.Type("aspire stop --non-interactive")
            .Enter()
            .WaitUntil(s => waitForNoRunningAppHostsFound.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForAnyPrompt(counter, TimeSpan.FromSeconds(30));

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task StopAllAppHostsFromAppHostDirectory()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        var waitForNoRunningAppHostsFound = new CellPatternSearcher()
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

        // Create first project
        sequenceBuilder.AspireNew("App1", counter);

        // Clear screen before second project creation
        sequenceBuilder.ClearScreen(counter);

        // Create second project
        sequenceBuilder.AspireNew("App2", counter);

        // Start first AppHost in background
        sequenceBuilder.Type("cd App1/App1.AppHost && aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Clear screen before starting second apphost
        sequenceBuilder.ClearScreen(counter);

        // Navigate back and start second AppHost in background
        sequenceBuilder.Type("cd ../../App2/App2.AppHost && aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Stop all AppHosts from within an AppHost directory using --non-interactive --all
        sequenceBuilder.Type("aspire stop --non-interactive --all")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Verify no AppHosts are running
        sequenceBuilder.Type("aspire stop --non-interactive")
            .Enter()
            .WaitUntil(s => waitForNoRunningAppHostsFound.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForAnyPrompt(counter, TimeSpan.FromSeconds(30));

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task StopAllAppHostsFromUnrelatedDirectory()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        var waitForNoRunningAppHostsFound = new CellPatternSearcher()
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

        // Create first project
        sequenceBuilder.AspireNew("App1", counter);

        // Clear screen before second project creation
        sequenceBuilder.ClearScreen(counter);

        // Create second project
        sequenceBuilder.AspireNew("App2", counter);

        // Start first AppHost in background
        sequenceBuilder.Type("cd App1/App1.AppHost && aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Clear screen before starting second apphost
        sequenceBuilder.ClearScreen(counter);

        // Navigate back and start second AppHost in background
        sequenceBuilder.Type("cd ../../App2/App2.AppHost && aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Navigate to workspace root (unrelated to any AppHost directory)
        sequenceBuilder.Type($"cd {workspace.WorkspaceRoot.FullName}")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Stop all AppHosts from an unrelated directory using --non-interactive --all
        sequenceBuilder.Type("aspire stop --non-interactive --all")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Verify no AppHosts are running
        sequenceBuilder.Type("aspire stop --non-interactive")
            .Enter()
            .WaitUntil(s => waitForNoRunningAppHostsFound.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForAnyPrompt(counter, TimeSpan.FromSeconds(30));

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task StopNonInteractiveMultipleAppHostsShowsError()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForMultipleAppHostsError = new CellPatternSearcher()
            .Find("Multiple AppHosts are running");

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

        // Create first project
        sequenceBuilder.AspireNew("App1", counter);

        // Clear screen before second project creation
        sequenceBuilder.ClearScreen(counter);

        // Create second project
        sequenceBuilder.AspireNew("App2", counter);

        // Start first AppHost in background
        sequenceBuilder.Type("cd App1/App1.AppHost && aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Clear screen before starting second apphost
        sequenceBuilder.ClearScreen(counter);

        // Navigate back and start second AppHost in background
        sequenceBuilder.Type("cd ../../App2/App2.AppHost && aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Navigate to workspace root
        sequenceBuilder.Type($"cd {workspace.WorkspaceRoot.FullName}")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Try to stop in non-interactive mode - should get an error about multiple AppHosts
        sequenceBuilder.Type("aspire stop --non-interactive")
            .Enter()
            .WaitUntil(s => waitForMultipleAppHostsError.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForAnyPrompt(counter, TimeSpan.FromSeconds(30));

        // Clear screen
        sequenceBuilder.ClearScreen(counter);

        // Now use --all to stop all AppHosts
        sequenceBuilder.Type("aspire stop --all")
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
