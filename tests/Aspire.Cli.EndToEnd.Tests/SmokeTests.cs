// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
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
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

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

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        sequenceBuilder.AspireNew("AspireStarterApp", counter)
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
