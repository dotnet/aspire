// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// Smoke tests that run inside a Docker container using Hex1b's WithDockerContainer API.
/// The test environment is built from a Dockerfile that can either compile the CLI from
/// source (local dev) or accept pre-built artifacts (CI). The test code detects the
/// environment and installs the CLI accordingly.
/// </summary>
public sealed class DockerSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task InstallAndVerifyAspireInDockerContainer()
    {
        _ = output;

        // The repo root is the Docker build context — needed for multi-stage Dockerfile
        // to COPY source files when building from source.
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var waitingForAspireVersion = new CellPatternSearcher()
            .FindPattern(@"\d+\.\d+\.\d+");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        // Set up prompt tracking inside the Docker container (handles root # prompt).
        sequenceBuilder.PrepareDockerEnvironment(counter);

        // Install the CLI using the method appropriate for the current environment.
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Verify the CLI reports a version number.
        sequenceBuilder
            .Type("aspire --version")
            .Enter()
            .WaitUntil(s => waitingForAspireVersion.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
