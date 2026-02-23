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

        // Pattern searcher for the apphost selection prompt
        var waitingForAppHostSelectionPrompt = new CellPatternSearcher()
            .Find("Select an apphost to use:");

        // After selecting, `aspire run` will try to build the single-file apphost.
        // We expect it to fail since these are minimal stubs, but that's fine —
        // we only need to verify the selection prompt appears.

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create two single-file AppHost directories so the CLI detects multiple apphosts.
        // A single-file apphost is a .cs file with "#:sdk Aspire.AppHost.Sdk" and no sibling .csproj.
        var appHost1Dir = Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost1");
        var appHost2Dir = Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost2");

        sequenceBuilder.ExecuteCallback(() =>
        {
            Directory.CreateDirectory(appHost1Dir);
            File.WriteAllText(Path.Combine(appHost1Dir, "apphost.cs"),
                """
                #:sdk Aspire.AppHost.Sdk
                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """);

            Directory.CreateDirectory(appHost2Dir);
            File.WriteAllText(Path.Combine(appHost2Dir, "apphost.cs"),
                """
                #:sdk Aspire.AppHost.Sdk
                var builder = DistributedApplication.CreateBuilder(args);
                builder.Build().Run();
                """);
        });

        // Run aspire run from the workspace root — should find both apphosts and prompt
        sequenceBuilder.Type("aspire run")
            .Enter()
            .WaitUntil(s => waitingForAppHostSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            // Select the first apphost by pressing Enter
            .Enter()
            // The selected apphost will attempt to build/run; it may fail since these
            // are minimal stubs. Wait for the shell prompt (success or error) either way.
            .WaitUntil(snapshot =>
            {
                var promptSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText("] $ ");

                return promptSearcher.Search(snapshot).Count > 0;
            }, TimeSpan.FromMinutes(2))
            .IncrementSequence(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
