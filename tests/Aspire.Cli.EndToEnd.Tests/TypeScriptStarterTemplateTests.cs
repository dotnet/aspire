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
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern for template selection - find "Starter App (Express/React)"
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        // Use Find() for literal string with parentheses/slashes
        var waitingForExpressReactTemplateSelected = new CellPatternSearcher()
            .Find("> Starter App (Express/React)");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find("Enter the project name (");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find("Enter the output path:");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find("Use *.dev.localhost URLs");

        var waitForDashboardCurlSuccess = new CellPatternSearcher()
            .Find("dashboard-http-200");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            // TypeScript starter requires the bundle (not just CLI) because the AppHost server
            // is bundled and cannot be obtained via NuGet packages in SDK-based fallback mode
            sequenceBuilder.InstallAspireBundleFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireBundleEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Create project using aspire new, selecting the Express/React template
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            // Navigate down to "Starter App (Express/React)" which is the 4th option
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .WaitUntil(s => waitingForExpressReactTemplateSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .Enter() // select "Starter App (Express/React)"
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("TsStarterApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default output path
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // select "No" for localhost URLs (default)
            .WaitForSuccessPrompt(counter);

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
