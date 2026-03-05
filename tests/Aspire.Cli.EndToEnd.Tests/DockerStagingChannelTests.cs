// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for staging channel configuration running inside a Docker container.
/// Verifies that staging settings are correctly persisted and that channel switching works.
/// </summary>
public sealed class DockerStagingChannelTests(ITestOutputHelper output)
{
    [Fact]
    public async Task StagingChannel_ConfigureAndVerifySettings_ThenSwitchChannels()
    {
        _ = output;

        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareDockerEnvironment(counter);
        sequenceBuilder.InstallAspireCliInDocker(installMode, counter);

        // Step 1: Configure staging channel settings via aspire config set
        sequenceBuilder
            .Type("aspire config set features.stagingChannelEnabled true -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire config set overrideStagingQuality Prerelease -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire config set stagingPinToCliVersion true -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("aspire config set channel staging -g")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 2: Verify settings were persisted in global settings file
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

        // Step 4: Verify the CLI still works with these settings
        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire --version")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 5: Switch channel to stable
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

        // Step 8: Verify channel is staging again and staging settings survived
        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire config get channel")
            .Enter()
            .WaitUntil(s => stagingChannelPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        var prereleasePattern = new CellPatternSearcher()
            .Find("Prerelease");

        sequenceBuilder
            .ClearScreen(counter)
            .Type("aspire config get overrideStagingQuality")
            .Enter()
            .WaitUntil(s => prereleasePattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // No cleanup needed — the container is ephemeral.
        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
