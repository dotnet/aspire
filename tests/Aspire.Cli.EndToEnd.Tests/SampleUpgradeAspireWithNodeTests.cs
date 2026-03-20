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

        // Determine the update channel. In PullRequest mode, explicitly pass the PR channel
        // so that aspire update uses the PR hive packages instead of stable nuget.org versions.
        string? updateChannel = installMode == CliE2ETestHelpers.DockerInstallMode.PullRequest
            ? $"pr-{CliE2ETestHelpers.GetRequiredPrNumber()}"
            : null;

        // Navigate to the sample directory first
        await auto.TypeAsync("cd aspire-samples/samples/aspire-with-node");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // In PullRequest mode, set up a NuGet.config with the PR hive source so that
        // dotnet add package (used by aspire update's apply phase) can resolve PR packages.
        if (updateChannel is not null)
        {
            await auto.SetupPrHiveNuGetConfigAsync(counter, updateChannel);
        }

        // Update the sample to the PR/CI build (already in the sample directory)
        await auto.AspireUpdateInSampleAsync(counter, samplePath: ".",
            channel: updateChannel, timeout: TimeSpan.FromMinutes(5));

        // Verify that the AppHost csproj was actually updated (no longer contains 13.1.0)
        await auto.VerifySampleWasUpgradedAsync(counter,
            "AspireWithNode.AppHost/AspireWithNode.AppHost.csproj",
            originalVersion: "13.1.0");

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
