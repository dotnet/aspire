// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for the TypeScript Express/React starter template (aspire-ts-starter).
/// Validates that aspire new creates a working Express API + React frontend project
/// and that aspire run starts it successfully.
/// </summary>
public sealed class TypeScriptStarterTemplateTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateAndRunTypeScriptStarterProject()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var waitForDashboardCurlSuccess = new CellPatternSearcher()
            .Find("dashboard-http-200");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter, workspace);

        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Step 1: Create project using aspire new, selecting the Express/React template
        sequenceBuilder.AspireNew("TsStarterApp", counter, template: AspireTemplate.ExpressReact);

        // Step 1.5: Verify starter creation also restored the generated TypeScript SDK.
        sequenceBuilder.ExecuteCallback(() =>
        {
            var projectRoot = Path.Combine(workspace.WorkspaceRoot.FullName, "TsStarterApp");
            var modulesDir = Path.Combine(projectRoot, ".modules");

            if (!Directory.Exists(modulesDir))
            {
                throw new InvalidOperationException($".modules directory was not created at {modulesDir}");
            }

            var aspireModulePath = Path.Combine(modulesDir, "aspire.ts");
            if (!File.Exists(aspireModulePath))
            {
                throw new InvalidOperationException($"Expected generated file not found: {aspireModulePath}");
            }
        });

        // Step 2: Navigate into the project and start it in background with JSON output
        sequenceBuilder
            .Type("cd TsStarterApp")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("aspire start --format json | tee /tmp/aspire-start.json")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(3))
            // Extract dashboard URL from JSON and curl it to verify it's reachable
            .Type("DASHBOARD_URL=$(sed -n 's/.*\"dashboardUrl\"[[:space:]]*:[[:space:]]*\"\\(https:\\/\\/localhost:[0-9]*\\).*/\\1/p' /tmp/aspire-start.json | head -1)")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("curl -ksSL -o /dev/null -w 'dashboard-http-%{http_code}' \"$DASHBOARD_URL\" || echo 'dashboard-http-failed'")
            .Enter()
            .WaitUntil(s => waitForDashboardCurlSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(15))
            .WaitForSuccessPrompt(counter)
            .Type("aspire stop")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
