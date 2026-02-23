// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI update command skill file detection and update.
/// </summary>
public sealed class UpdateSkillFileTests(ITestOutputHelper output)
{
    private static readonly (string RelativeDir, string AgentName)[] s_skillFileLocations =
    [
        (Path.Combine(".github", "skills", "aspire"), "GitHub Copilot"),
        (Path.Combine(".opencode", "skill", "aspire"), "OpenCode"),
        (Path.Combine(".claude", "skills", "aspire"), "Claude Code"),
    ];

    /// <summary>
    /// Tests that `aspire update` detects outdated SKILL.md files at all locations and prompts to update each.
    /// The test creates placeholder SKILL.md files at all three agent locations (.github, .opencode, .claude),
    /// runs `aspire update`, and verifies that prompts appear for each outdated file.
    /// </summary>
    [Fact]
    public async Task UpdateCommand_DetectsOutdatedSkillFiles_AllLocations()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(UpdateCommand_DetectsOutdatedSkillFiles_AllLocations));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Compute paths for all skill files
        var skillFilePaths = s_skillFileLocations
            .Select(loc => Path.Combine(workspace.WorkspaceRoot.FullName, loc.RelativeDir, "SKILL.md"))
            .ToArray();

        // Pattern to detect the skill file update prompts
        var outOfDatePrompt = new CellPatternSearcher().Find("out of date");

        // Pattern for successful skill file update
        var skillFileUpdatedMessage = new CellPatternSearcher().Find("skill file updated");

        // Patterns for aspire new prompts (same as SmokeTests)
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find("Enter the project name");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find("Enter the output path:");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find("Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find("Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find("Do you want to create a test project?");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Initialize git repository (required for skill file detection)
        sequenceBuilder
            .Type("git init")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Create all three outdated skill files
        sequenceBuilder.ExecuteCallback(() =>
        {
            foreach (var (relativeDir, agentName) in s_skillFileLocations)
            {
                var skillFileDir = Path.Combine(workspace.WorkspaceRoot.FullName, relativeDir);
                var skillFilePath = Path.Combine(skillFileDir, "SKILL.md");
                Directory.CreateDirectory(skillFileDir);
                File.WriteAllText(skillFilePath, $"# Outdated {agentName} Skill\n\nThis is placeholder content that is outdated.");
            }
        });

        // Verify all skill files were created
        var fileExistsPattern = new CellPatternSearcher().Find("SKILL.md");
        sequenceBuilder
            .Type($"find {workspace.WorkspaceRoot.FullName} -name 'SKILL.md'")
            .Enter()
            .WaitUntil(s => fileExistsPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Create an Aspire project using the same flow as SmokeTests
        sequenceBuilder
            .Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select first template (Starter App)
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("TestApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default output path
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default for URLs
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default for Redis
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default for test project
            .WaitForSuccessPrompt(counter);

        // Clear the screen to avoid pattern interference from previous commands
        sequenceBuilder.ClearScreen(counter);

        // Pattern for channel selection prompt that appears during aspire update
        var waitingForChannelPrompt = new CellPatternSearcher().Find("Select a channel:");

        // Run aspire update - should detect all outdated skill files and prompt for each
        sequenceBuilder
            .Type("aspire update")
            .Enter()
            // First, handle the channel selection prompt
            .WaitUntil(s => waitingForChannelPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Enter(); // select default channel

        // Pattern searchers for each specific location
        var githubPrompt = new CellPatternSearcher().Find(".github");
        var opencodePrompt = new CellPatternSearcher().Find(".opencode");
        var claudePrompt = new CellPatternSearcher().Find(".claude");

        // Handle the .github skill file prompt (first)
        sequenceBuilder
            .WaitUntil(s => githubPrompt.Search(s).Count > 0 && outOfDatePrompt.Search(s).Count > 0, TimeSpan.FromMinutes(2))
            .Type("y")
            .Enter()
            .WaitUntil(s => skillFileUpdatedMessage.Search(s).Count > 0, TimeSpan.FromSeconds(30));

        // Handle the .opencode skill file prompt (second)
        sequenceBuilder
            .WaitUntil(s => opencodePrompt.Search(s).Count > 0 && outOfDatePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Type("y")
            .Enter()
            .WaitUntil(s => skillFileUpdatedMessage.Search(s).Count > 0, TimeSpan.FromSeconds(30));

        // Handle the .claude skill file prompt (third)
        sequenceBuilder
            .WaitUntil(s => claudePrompt.Search(s).Count > 0 && outOfDatePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Type("y")
            .Enter()
            .WaitUntil(s => skillFileUpdatedMessage.Search(s).Count > 0, TimeSpan.FromSeconds(30));

        sequenceBuilder.WaitForSuccessPrompt(counter);

        // Verify all skill files were updated with the new content
        foreach (var skillFilePath in skillFilePaths)
        {
            sequenceBuilder.VerifyFileContains(skillFilePath, "# Aspire Skill");
            sequenceBuilder.VerifyFileDoesNotContain(skillFilePath, "Outdated");
        }

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
