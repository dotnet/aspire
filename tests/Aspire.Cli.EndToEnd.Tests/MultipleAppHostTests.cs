// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Tests that <c>aspire start --format json</c> produces well-formed JSON
/// without human-readable messages polluting stdout.
/// </summary>
public sealed class MultipleAppHostTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DetachFormatJsonProducesValidJson()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Create a single project using aspire new
        sequenceBuilder.AspireNew("TestApp", counter);

        sequenceBuilder.ClearScreen(counter);

        // Navigate into the project directory
        sequenceBuilder
            .Type("cd TestApp")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // First: launch the apphost with --detach (interactive, no JSON)
        // Just wait for the command to complete (WaitForSuccessPrompt waits for the shell prompt)
        sequenceBuilder
            .Type("aspire start")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ClearScreen(counter);

        // Second: launch again with --detach --format json, redirecting stdout to a file.
        // This tests that the JSON output is well-formed and not polluted by human-readable messages.
        // stderr is left visible in the terminal for debugging (human-readable messages go to stderr
        // when --format json is used, which is exactly what this PR validates).
        sequenceBuilder
            .Type("aspire start --format json > output.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.ClearScreen(counter);

        // Validate the JSON output file is well-formed by using python to parse it
        sequenceBuilder
            .Type("python3 -c \"import json; data = json.load(open('output.json')); print('JSON_VALID'); print('appHostPath' in data); print('appHostPid' in data)\"")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Also cat the file so we can see it in the recording
        sequenceBuilder
            .Type("cat output.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Clean up: stop any running instances
        sequenceBuilder
            .Type("aspire stop --all 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}

