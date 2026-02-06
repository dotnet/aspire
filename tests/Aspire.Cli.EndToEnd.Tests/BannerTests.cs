// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
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
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(Banner_DisplayedOnFirstRun));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect the ASPIRE banner text (the welcome message)
        // The banner displays "Welcome to the" followed by ASCII art "ASPIRE"
        var bannerPattern = new CellPatternSearcher()
            .Find("Welcome to the");

        // Pattern to detect the telemetry notice (shown on first run)
        var telemetryNoticePattern = new CellPatternSearcher()
            .Find("Telemetry");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Delete the first-time use sentinel file to simulate first run
        // The sentinel is stored at ~/.aspire/cli/cli.firstUseSentinel
        // Using 'aspire cache clear' because it's not an informational
        // command and so will show the banner.
        sequenceBuilder
            .ClearFirstRunSentinel(counter)
            .VerifySentinelDeleted(counter)
            .ClearScreen(counter)
            .Type("aspire cache clear")
            .Enter()
            .WaitUntil(s =>
            {
                // Verify the banner appears
                var hasBanner = bannerPattern.Search(s).Count > 0;
                var hasTelemetryNotice = telemetryNoticePattern.Search(s).Count > 0;

                // Both should appear on first run
                return hasBanner && hasTelemetryNotice;
            }, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task Banner_DisplayedWithExplicitFlag()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(Banner_DisplayedWithExplicitFlag));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

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
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Clear screen to have a clean slate for pattern matching
        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire --banner")
            .Enter()
            .WaitUntil(s =>
            {
                // Verify the banner appears with version info
                var hasBanner = bannerPattern.Search(s).Count > 0;
                var hasVersion = versionPattern.Search(s).Count > 0;

                return hasBanner && hasVersion;
            }, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/14307")]
    public async Task Banner_NotDisplayedWithNoLogoFlag()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(Banner_NotDisplayedWithNoLogoFlag));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern to detect the ASPIRE banner - should NOT appear
        // The banner displays "Welcome to the" followed by ASCII art "ASPIRE"
        var bannerPattern = new CellPatternSearcher()
            .Find("Welcome to the");

        // Pattern to detect the help text (confirms command completed)
        var helpPattern = new CellPatternSearcher()
            .Find("Commands:");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Delete the first-time use sentinel file to simulate first run,
        // but use --nologo to suppress the banner
        sequenceBuilder
            .ClearFirstRunSentinel(counter)
            .ClearScreen(counter)
            .Type("aspire --nologo --help")
            .Enter()
            .WaitUntil(s =>
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
            }, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter)
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
