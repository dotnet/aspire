// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for running .NET csproj AppHost projects using the Aspire bundle.
/// Validates that the bundle correctly provides DCP and Dashboard paths to the hosting
/// infrastructure when running SDK-based app hosts (not just polyglot/guest app hosts).
/// </summary>
public sealed class BundleSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateAndRunAspireStarterProjectWithBundle()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateAndRunAspireStarterProjectWithBundle));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./BundleStarterApp): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find($"Do you want to create a test project?");

        // Verify the dashboard is actually reachable, not just that the URL was printed.
        // When the dashboard path bug was present, the URL appeared on screen but curling
        // it returned connection refused because the dashboard process failed to start.
        var waitForDashboardCurlSuccess = new CellPatternSearcher()
            .Find("dashboard-http-200");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            // Install the full bundle (not just CLI) so that ASPIRE_LAYOUT_PATH is set.
            // For .NET csproj app hosts, the hosting infrastructure resolves DCP and Dashboard
            // paths through NuGet assembly metadata, NOT through bundle env vars.
            sequenceBuilder.InstallAspireBundleFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireBundleEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select first template (Starter App)
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("BundleStarterApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitForSuccessPrompt(counter)
            // Diagnostic: show bundle layout and environment state
            .Type("ls -la ~/.aspire/ && echo '---' && ls -la ~/.aspire/bin/ 2>/dev/null && echo '---' && echo ASPIRE_DCP_PATH=$ASPIRE_DCP_PATH && echo ASPIRE_DASHBOARD_PATH=$ASPIRE_DASHBOARD_PATH && echo ASPIRE_LAYOUT_PATH=$ASPIRE_LAYOUT_PATH")
            .Enter()
            .WaitForSuccessPrompt(counter)
            // Use --detach --debug to get CLI debug output for diagnosing dashboardUrl:null
            .Type("aspire run --detach --format json 2>/tmp/aspire-detach-stderr.log | tee /tmp/aspire-detach.json")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(3))
            // Show stderr from detach for debugging
            .Type("cat /tmp/aspire-detach-stderr.log 2>/dev/null | tail -50")
            .Enter()
            .WaitForSuccessPrompt(counter)
            // Verify the dashboard is reachable by extracting the URL from aspire ps
            // and curling it. aspire ps --format json returns a JSON array with dashboardUrl.
            // Extract just the base URL (https://localhost:PORT) using sed, which is
            // portable across macOS (BSD) and Linux (GNU) unlike grep -oP.
            .Type("DASHBOARD_URL=$(aspire ps --format json | sed -n 's/.*\"dashboardUrl\"[[:space:]]*:[[:space:]]*\"\\(https:\\/\\/localhost:[0-9]*\\).*/\\1/p' | head -1)")
            .Enter()
            .WaitForSuccessPrompt(counter)
            // Diagnostic: show the CLI log file to understand why dashboardUrl may be null
            .Type("cat /tmp/aspire-detach.json && echo && aspire ps --format json")
            .Enter()
            .WaitForSuccessPrompt(counter)
            // Show the child CLI's log file for dashboard startup diagnostics
            .Type("LOG_FILE=$(cat /tmp/aspire-detach.json | sed -n 's/.*\"logFile\"[[:space:]]*:[[:space:]]*\"\\([^\"]*\\)\".*/\\1/p') && echo \"Log file: $LOG_FILE\" && cat \"$LOG_FILE\" 2>/dev/null | tail -100")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("curl -ksSL -o /dev/null -w 'dashboard-http-%{http_code}' \"$DASHBOARD_URL\" || echo 'dashboard-http-failed'")
            .Enter()
            .WaitUntil(s => waitForDashboardCurlSuccess.Search(s).Count > 0, TimeSpan.FromSeconds(15))
            .WaitForSuccessPrompt(counter)
            // Clean up: use aspire stop to gracefully shut down the detached AppHost.
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
