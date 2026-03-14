// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI banner display functionality.
/// These tests verify that the banner appears on first run and when explicitly requested.
/// </summary>
public sealed class BannerTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Banner_DisplayedOnFirstRun()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect the ASPIRE banner text (the welcome message)
        // The banner displays "Welcome to the" followed by ASCII art "ASPIRE"
        var bannerPattern = new CellPatternSearcher()
            .Find("Welcome to the");

        // Pattern to detect the telemetry notice (shown on first run)
        var telemetryNoticePattern = new CellPatternSearcher()
            .Find("Telemetry");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Delete the first-time use sentinel file to simulate first run
        // The sentinel is stored at ~/.aspire/cli/cli.firstUseSentinel
        // Using 'aspire cache clear' because it's not an informational
        // command and so will show the banner.
        await auto.TypeAsync("rm -f ~/.aspire/cli/cli.firstUseSentinel");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("test ! -f ~/.aspire/cli/cli.firstUseSentinel");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.ClearScreenAsync(counter);
        await auto.TypeAsync("aspire cache clear");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            // Verify the banner appears
            var hasBanner = bannerPattern.Search(s).Count > 0;
            var hasTelemetryNotice = telemetryNoticePattern.Search(s).Count > 0;

            // Both should appear on first run
            return hasBanner && hasTelemetryNotice;
        }, timeout: TimeSpan.FromSeconds(30), description: "waiting for banner and telemetry notice on first run");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    public async Task Banner_DisplayedWithExplicitFlag()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect the ASPIRE banner welcome text
        // The banner displays "Welcome to the" followed by ASCII art "ASPIRE"
        var bannerPattern = new CellPatternSearcher()
            .Find("Welcome to the");

        // Pattern to detect version info in the banner
        // The format is "CLI — version X.Y.Z"
        var versionPattern = new CellPatternSearcher()
            .Find("CLI");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Clear screen to have a clean slate for pattern matching
        await auto.ClearScreenAsync(counter);
        await auto.TypeAsync("aspire --banner");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            // Verify the banner appears with version info
            var hasBanner = bannerPattern.Search(s).Count > 0;
            var hasVersion = versionPattern.Search(s).Count > 0;

            return hasBanner && hasVersion;
        }, timeout: TimeSpan.FromSeconds(30), description: "waiting for banner with version info");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/14307")]
    public async Task Banner_NotDisplayedWithNoLogoFlag()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect the ASPIRE banner - should NOT appear
        // The banner displays "Welcome to the" followed by ASCII art "ASPIRE"
        var bannerPattern = new CellPatternSearcher()
            .Find("Welcome to the");

        // Pattern to detect the help text (confirms command completed)
        var helpPattern = new CellPatternSearcher()
            .Find("Commands:");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Delete the first-time use sentinel file to simulate first run,
        // but use --nologo to suppress the banner
        await auto.TypeAsync("rm -f ~/.aspire/cli/cli.firstUseSentinel");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.ClearScreenAsync(counter);
        await auto.TypeAsync("aspire --nologo --help");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            // Wait for help output to confirm command completed
            var hasHelp = helpPattern.Search(s).Count > 0;
            if (!hasHelp)
            {
                return false;
            }

            // Verify the banner does NOT appear
            var hasBanner = bannerPattern.Search(s).Count > 0;
            if (hasBanner)
            {
                throw new InvalidOperationException(
                    "Unexpected banner displayed when --nologo flag was used!");
            }

            return true;
        }, timeout: TimeSpan.FromSeconds(30), description: "waiting for help output without banner");
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
