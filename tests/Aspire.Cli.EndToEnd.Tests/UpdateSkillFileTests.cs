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
    /// <summary>
    /// Tests that `aspire update` detects an outdated SKILL.md file and prompts to update it.
    /// The test creates a placeholder SKILL.md file with outdated content, runs `aspire update`,
    /// and verifies that the prompt to update the skill file appears.
    /// </summary>
    [Fact]
    public async Task UpdateCommand_DetectsOutdatedSkillFile_PromptsForUpdate()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(UpdateCommand_DetectsOutdatedSkillFile_PromptsForUpdate));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(160, 48)
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Create paths for the outdated skill file
        var skillFileDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".github", "skills", "aspire");
        var skillFilePath = Path.Combine(skillFileDir, "SKILL.md");

        // Pattern to detect the skill file update prompt
        var skillFileUpdatePrompt = new CellPatternSearcher().Find("SKILL.md");
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

        // Create the outdated skill file with placeholder content
        sequenceBuilder.ExecuteCallback(() =>
        {
            Directory.CreateDirectory(skillFileDir);
            File.WriteAllText(skillFilePath, "# Outdated Aspire Skill\n\nThis is placeholder content that is outdated.");
        });

        // Verify the skill file was created
        var fileExistsPattern = new CellPatternSearcher().Find("SKILL.md");
        sequenceBuilder
            .Type($"ls -la {skillFilePath}")
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

        // Run aspire update - should detect the outdated skill file and prompt for update
        sequenceBuilder
            .Type("aspire update")
            .Enter()
            .WaitUntil(s =>
            {
                // Wait for the skill file prompt to appear
                var hasSkillPrompt = skillFileUpdatePrompt.Search(s).Count > 0;
                var hasOutOfDate = outOfDatePrompt.Search(s).Count > 0;
                return hasSkillPrompt && hasOutOfDate;
            }, TimeSpan.FromMinutes(2));

        // Confirm the skill file update (press 'y')
        sequenceBuilder
            .Type("y")
            .Enter()
            .WaitUntil(s => skillFileUpdatedMessage.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitForSuccessPrompt(counter);

        // Verify the skill file was updated with the new content
        sequenceBuilder.VerifyFileContains(skillFilePath, "# Aspire Skill");
        sequenceBuilder.VerifyFileDoesNotContain(skillFilePath, "Outdated Aspire Skill");

        sequenceBuilder
            .Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
