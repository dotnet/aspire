// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
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
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Patterns for prompt detection
        var workspacePrompt = new CellPatternSearcher().Find("workspace:");
        var agentEnvPrompt = new CellPatternSearcher().Find("agent environments");
        var additionalOptionsPrompt = new CellPatternSearcher().Find("additional options");
        var playwrightOption = new CellPatternSearcher().Find("Install Playwright CLI");
        var configComplete = new CellPatternSearcher().Find("configuration complete");
        var skillFileExists = new CellPatternSearcher().Find("SKILL.md");

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Step 1: Verify playwright-cli is not installed.
        await auto.TypeAsync("playwright-cli --version 2>&1 || true");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 2: Create an Aspire project (accept all defaults).
        await auto.AspireNewAsync("TestProject", counter);

        // Step 3: Navigate into the project and create .claude folder to trigger Claude Code detection.
        await auto.TypeAsync("cd TestProject && mkdir -p .claude");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 4: Run aspire agent init.
        // First prompt: workspace path
        await auto.TypeAsync("aspire agent init");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => workspacePrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for workspace prompt");
        await Task.Delay(500);
        await auto.EnterAsync(); // Accept default workspace path

        // Second prompt: agent environments (select Claude Code)
        await auto.WaitUntilAsync(s => agentEnvPrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(60), description: "waiting for agent environments prompt");
        await auto.TypeAsync(" "); // Toggle first option (Claude Code)
        await auto.EnterAsync();

        // Third prompt: additional options (select Playwright CLI installation)
        // Aspire skill file (priority 0) appears first, Playwright CLI (priority 1) second.
        await auto.WaitUntilAsync(s => additionalOptionsPrompt.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(30), description: "waiting for additional options prompt");
        await auto.WaitUntilAsync(s => playwrightOption.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(10), description: "waiting for Playwright CLI option");
        await auto.TypeAsync(" "); // Toggle first option (Aspire skill file)
        await auto.DownAsync(); // Move to Playwright CLI option
        await auto.TypeAsync(" "); // Toggle Playwright CLI option
        await auto.EnterAsync();

        // Wait for installation to complete (this downloads from npm, can take a while)
        await auto.WaitUntilAsync(s => configComplete.Search(s).Count > 0, timeout: TimeSpan.FromMinutes(3), description: "waiting for configuration complete");
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 5: Verify playwright-cli is now installed.
        await auto.TypeAsync("playwright-cli --version");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Step 6: Verify the skill file was generated.
        await auto.TypeAsync("ls .claude/skills/playwright-cli/SKILL.md");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s => skillFileExists.Search(s).Count > 0, timeout: TimeSpan.FromSeconds(10), description: "waiting for SKILL.md file listing");
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
