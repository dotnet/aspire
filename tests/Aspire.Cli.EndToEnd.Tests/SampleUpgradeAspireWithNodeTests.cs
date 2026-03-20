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
    private const string SamplePath = "aspire-samples/samples/aspire-with-node";
    private const string AppHostCsproj = "AspireWithNode.AppHost/AspireWithNode.AppHost.csproj";
    private const string OriginalVersion = "13.1.0";

    [Fact]
    public async Task UpgradeAndRunAspireWithNodeSample()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        // Mount a host-side working directory into the container so we can
        // inspect files directly from the test process after the upgrade.
        var workDir = Path.Combine(workspace.WorkspaceRoot.FullName, "sample-work");
        Directory.CreateDirectory(workDir);
        const string containerWorkDir = "/sample-work";

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(
            repoRoot, installMode, output,
            mountDockerSocket: true,
            workspace: workspace,
            additionalVolumes: [$"{workDir}:{containerWorkDir}"]);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(600));

        // Prepare Docker environment (prompt counting, umask, env vars)
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        // Install the Aspire CLI
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Clone the aspire-samples repository into the mounted working directory
        await auto.TypeAsync($"cd {containerWorkDir}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.CloneSampleRepoAsync(counter);

        // Navigate to the sample directory
        await auto.TypeAsync($"cd {SamplePath}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Phase 1: Run aspire update to perform structural migration (SDK format, etc.)
        // This uses the implicit channel and updates to the latest stable version.
        await auto.AspireUpdateInSampleAsync(counter, samplePath: ".",
            timeout: TimeSpan.FromMinutes(5));

        // Phase 2: In PullRequest mode, upgrade all Aspire package versions to the PR build.
        // aspire update --channel has issues with dotnet package add and PR hive sources,
        // so we do a direct version replacement and set up the NuGet.config for the hive.
        if (installMode == CliE2ETestHelpers.DockerInstallMode.PullRequest)
        {
            var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
            var channel = $"pr-{prNumber}";

            await auto.SetupPrHiveNuGetConfigAsync(counter, channel);
            await auto.UpgradeToPrVersionAsync(counter, channel, AppHostCsproj);
        }

        // Verify the upgrade by reading the csproj directly from the mounted volume
        var hostCsprojPath = Path.Combine(workDir, SamplePath, AppHostCsproj);
        var csprojContent = await File.ReadAllTextAsync(hostCsprojPath);
        output.WriteLine($"--- AppHost csproj after upgrade ---");
        output.WriteLine(csprojContent);

        Assert.DoesNotContain(OriginalVersion, csprojContent);

        if (installMode == CliE2ETestHelpers.DockerInstallMode.PullRequest)
        {
            Assert.Contains("-pr.", csprojContent);
        }

        // Run the sample
        await auto.AspireRunSampleAsync(
            appHostRelativePath: AppHostCsproj,
            startTimeout: TimeSpan.FromMinutes(5));

        // Stop the running apphost
        await auto.StopAspireRunAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
