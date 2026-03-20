// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// E2E test that clones the aspire-with-node sample from dotnet/aspire-samples,
/// upgrades it to the PR/CI build using <c>aspire update</c>, and verifies it runs correctly.
/// The sample consists of a Node.js Express frontend, an ASP.NET Core weather API, and Redis.
/// </summary>
public sealed class SampleUpgradeAspireWithNodeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task UpgradeAndRunAspireWithNodeSample()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(
            repoRoot, installMode, output,
            mountDockerSocket: true,
            workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(600));

        // Prepare Docker environment (prompt counting, umask, env vars)
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        // Install the Aspire CLI
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Clone the aspire-samples repository
        await auto.CloneSampleRepoAsync(counter);

        // Update the aspire-with-node sample to the PR/CI build
        await auto.AspireUpdateInSampleAsync(counter, "aspire-samples/samples/aspire-with-node");

        // Run the sample — the AppHost csproj is in the AspireWithNode.AppHost subdirectory
        await auto.AspireRunSampleAsync(
            appHostRelativePath: "AspireWithNode.AppHost/AspireWithNode.AppHost.csproj",
            startTimeout: TimeSpan.FromMinutes(5));

        // Stop the running apphost
        await auto.StopAspireRunAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
