// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Aspire.TestUtilities;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end test verifying that the Playwright CLI installation flow works correctly
/// through <c>aspire agent init</c>, including npm provenance verification and skill file generation.
/// </summary>
[OuterloopTest("Requires npm and network access to install @playwright/cli from the npm registry")]
public sealed class PlaywrightCliInstallTests(ITestOutputHelper output)
{
    /// <summary>
    /// Verifies the full Playwright CLI installation lifecycle:
    /// 1. Playwright CLI is not initially installed
    /// 2. An Aspire project is created
    /// 3. <c>aspire agent init</c> is run with Claude Code environment selected
    /// 4. Playwright CLI is installed and available on PATH
    /// 5. The <c>.claude/skills/playwright-cli/SKILL.md</c> skill file is generated
    /// </summary>
    [Fact]
    public async Task AgentInit_InstallsPlaywrightCli_AndGeneratesSkillFiles()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(AgentInit_InstallsPlaywrightCli_AndGeneratesSkillFiles));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Patterns for prompt detection
        var workspacePrompt = new CellPatternSearcher().Find("workspace:");
        var agentEnvPrompt = new CellPatternSearcher().Find("agent environments");
        var additionalOptionsPrompt = new CellPatternSearcher().Find("additional options");
        var playwrightOption = new CellPatternSearcher().Find("Install Playwright CLI");
        var configComplete = new CellPatternSearcher().Find("configuration complete");
        var skillFileExists = new CellPatternSearcher().Find("SKILL.md");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Verify playwright-cli is not installed.
        sequenceBuilder
            .Type("playwright-cli --version 2>&1 || true")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 2: Create an Aspire project (accept all defaults).
        var starterAppTemplate = new CellPatternSearcher().FindPattern("> Starter App");
        var projectNamePrompt = new CellPatternSearcher().Find("Enter the project name");
        var outputPathPrompt = new CellPatternSearcher().Find("Enter the output path");
        var urlsPrompt = new CellPatternSearcher().Find("*.dev.localhost URLs");
        var redisPrompt = new CellPatternSearcher().Find("Use Redis Cache");
        var testProjectPrompt = new CellPatternSearcher().Find("Do you want to create a test project?");

        sequenceBuilder
            .Type("aspire new")
            .Enter()
            .WaitUntil(s => starterAppTemplate.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Enter() // Select Starter App template
            .WaitUntil(s => projectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Type("TestProject")
            .Enter()
            .WaitUntil(s => outputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // Accept default output path
            .WaitUntil(s => urlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // Accept default URL setting
            .WaitUntil(s => redisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // Accept default Redis setting
            .WaitUntil(s => testProjectPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // Accept default test project setting
            .WaitForSuccessPrompt(counter);

        // Step 3: Navigate into the project and create .claude folder to trigger Claude Code detection.
        sequenceBuilder
            .Type("cd TestProject && mkdir -p .claude")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 4: Run aspire agent init.
        // First prompt: workspace path
        sequenceBuilder
            .Type("aspire agent init")
            .Enter()
            .WaitUntil(s => workspacePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Wait(500)
            .Enter(); // Accept default workspace path

        // Second prompt: agent environments (select Claude Code)
        sequenceBuilder
            .WaitUntil(s => agentEnvPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Type(" ") // Toggle first option (Claude Code)
            .Enter();

        // Third prompt: additional options (select Playwright CLI installation)
        // Aspire skill file (priority 0) appears first, Playwright CLI (priority 1) second.
        sequenceBuilder
            .WaitUntil(s => additionalOptionsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitUntil(s => playwrightOption.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type(" ") // Toggle first option (Aspire skill file)
            .Key(Hex1b.Input.Hex1bKey.DownArrow) // Move to Playwright CLI option
            .Type(" ") // Toggle Playwright CLI option
            .Enter();

        // Wait for installation to complete (this downloads from npm, can take a while)
        sequenceBuilder
            .WaitUntil(s => configComplete.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Step 5: Verify playwright-cli is now installed.
        sequenceBuilder
            .Type("playwright-cli --version")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 6: Verify the skill file was generated.
        sequenceBuilder
            .Type("ls .claude/skills/playwright-cli/SKILL.md")
            .Enter()
            .WaitUntil(s => skillFileExists.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
