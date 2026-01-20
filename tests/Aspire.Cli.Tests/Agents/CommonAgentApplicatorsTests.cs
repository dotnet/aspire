// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Agents;

public class CommonAgentApplicatorsTests(ITestOutputHelper outputHelper)
{
    private const string TestSkillRelativePath = ".github/skills/aspire/SKILL.md";
    private const string TestDescription = "Create Aspire skill file";

    [Fact]
    public void TryAddSkillFileApplicator_WhenNotYetAdded_AddsApplicatorAndReturnsTrue()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        // Act
        var result = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            TestSkillRelativePath,
            TestDescription);

        // Assert
        Assert.True(result);
        Assert.True(context.HasSkillFileApplicator(TestSkillRelativePath));
        Assert.Single(context.Applicators);
    }

    [Fact]
    public void TryAddSkillFileApplicator_WhenAlreadyAdded_DoesNotAddAgainAndReturnsFalse()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        context.MarkSkillFileApplicatorAdded(TestSkillRelativePath);

        // Act
        var result = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            TestSkillRelativePath,
            TestDescription);

        // Assert
        Assert.False(result);
        Assert.Empty(context.Applicators);
    }

    [Fact]
    public void TryAddSkillFileApplicator_WhenSkillFileExists_DoesNotAddApplicator()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        
        // Create the skill file with any content
        var skillFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, TestSkillRelativePath);
        var skillDirectory = Path.GetDirectoryName(skillFilePath)!;
        Directory.CreateDirectory(skillDirectory);
        File.WriteAllText(skillFilePath, "# Existing Content\n\nThis already exists.");

        // Act
        var result = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            TestSkillRelativePath,
            TestDescription);

        // Assert - should not add applicator since skill file already exists
        Assert.False(result);
        Assert.True(context.HasSkillFileApplicator(TestSkillRelativePath));
        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task TryAddSkillFileApplicator_CreatesSkillFileWhenItDoesNotExist()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        // Act
        CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            TestSkillRelativePath,
            TestDescription);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        // Assert
        var skillFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, TestSkillRelativePath);
        Assert.True(File.Exists(skillFilePath));
        var content = await File.ReadAllTextAsync(skillFilePath);
        Assert.Contains("# Aspire Skill", content);
        Assert.Contains("Running Aspire in agent environments", content);
    }

    [Fact]
    public void TryAddSkillFileApplicator_DifferentPaths_AddsBothApplicators()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        var path1 = ".github/skills/aspire/SKILL.md";
        var path2 = ".claude/skills/aspire/SKILL.md";

        // Act
        var result1 = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            path1,
            "Description 1");
        var result2 = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            path2,
            "Description 2");

        // Assert - both should be added since they are different paths
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(2, context.Applicators.Count);
    }

    [Fact]
    public void TryAddSkillFileApplicator_SamePathTwice_OnlyAddsOnce()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        var path = ".github/skills/aspire/SKILL.md";

        // Act - try to add the same path twice (simulating VS Code and Copilot CLI both trying to add)
        var result1 = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            path,
            "Description 1");
        var result2 = CommonAgentApplicators.TryAddSkillFileApplicator(
            context,
            workspace.WorkspaceRoot,
            path,
            "Description 2");

        // Assert - only the first should be added
        Assert.True(result1);
        Assert.False(result2);
        Assert.Single(context.Applicators);
    }

    private static AgentEnvironmentScanContext CreateScanContext(DirectoryInfo workingDirectory)
    {
        return new AgentEnvironmentScanContext
        {
            WorkingDirectory = workingDirectory,
            RepositoryRoot = workingDirectory
        };
    }
}
