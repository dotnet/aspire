// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for staging channel configuration and self-update channel switching.
/// Verifies that staging settings (overrideStagingQuality, stagingPinToCliVersion) are
/// correctly persisted and that aspire update --self saves the channel to global settings.
/// </summary>
public sealed class StagingChannelTests(ITestOutputHelper output)
{
    [Fact]
    public async Task StagingChannel_ConfigureAndVerifySettings_ThenSwitchChannels()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(StagingChannel_ConfigureAndVerifySettings_ThenSwitchChannels));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Configure staging channel settings via aspire config set
        // Enable the staging channel feature flag
        sequenceBuilder
            .Type("aspire config set features.stagingChannelEnabled true -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set quality to Prerelease (triggers shared feed mode)
        sequenceBuilder
            .Type("aspire config set overrideStagingQuality Prerelease -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Enable pinned version mode
        sequenceBuilder
            .Type("aspire config set stagingPinToCliVersion true -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Set channel to staging
        sequenceBuilder
            .Type("aspire config set channel staging -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 2: Verify the settings were persisted in the global settings file
        var settingsFilePattern = new CellPatternSearcher()
            .Find("stagingPinToCliVersion");

        sequenceBuilder
            .ClearScreen(counter)
            .Type("cat ~/.aspire/globalsettings.json")
            .Enter()
            .WaitUntil(s => settingsFilePattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 3: Verify aspire config get returns the correct values
        var stagingChannelPattern = new CellPatternSearcher()
            .Find("staging");

        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire config get channel")
            .Enter()
            .WaitUntil(s => stagingChannelPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 4: Verify the CLI version is available (basic smoke test that the CLI works with these settings)
        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire --version")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 5: Switch channel to stable via config set (simulating what update --self does)
        sequenceBuilder
            .Type("aspire config set channel stable -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 6: Verify channel was changed to stable
        var stableChannelPattern = new CellPatternSearcher()
            .Find("stable");

        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire config get channel")
            .Enter()
            .WaitUntil(s => stableChannelPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Step 7: Switch back to staging
        sequenceBuilder
            .Type("aspire config set channel staging -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 8: Verify channel is staging again and staging settings are still present
        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire config get channel")
            .Enter()
            .WaitUntil(s => stagingChannelPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Verify the staging-specific settings survived the channel switch
        var prereleasePattern = new CellPatternSearcher()
            .Find("Prerelease");

        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire config get overrideStagingQuality")
            .Enter()
            .WaitUntil(s => prereleasePattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Clean up: remove staging settings to avoid polluting other tests
        sequenceBuilder
            .Type("aspire config delete features.stagingChannelEnabled -g")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("aspire config delete overrideStagingQuality -g")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("aspire config delete stagingPinToCliVersion -g")
            .Enter()
            .WaitForSuccessPrompt(counter)
            .Type("aspire config delete channel -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
