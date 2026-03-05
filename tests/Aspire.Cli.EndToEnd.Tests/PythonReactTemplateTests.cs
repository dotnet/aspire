// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI with Python/React (FastAPI/Vite) template.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class PythonReactTemplateTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateAndRunPythonReactProject()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var waitForCtrlCMessage = new CellPatternSearcher()
            .Find($"Press CTRL+C to stop the apphost and exit.");

        // The purpose of this is to keep track of the number of actual shell commands we have
        // executed. This is important because we customize the shell prompt to show either
        // "[n OK] $ " or "[n ERR:exitcode] $ ". This allows us to deterministically wait for a
        // command to complete and for the shell to be ready for more input rather than relying
        // on arbitrary timeouts of mid-command strings.
        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        sequenceBuilder.AspireNew("AspirePyReactApp", counter, template: AspireTemplate.PythonReact, useRedisCache: false);

        sequenceBuilder
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
