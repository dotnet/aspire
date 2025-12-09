// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Agents;

public class CommonAgentApplicatorsTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void TryAddAgentInstructionsApplicator_WhenNotYetAdded_AddsApplicatorAndReturnsTrue()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        // Act
        var result = CommonAgentApplicators.TryAddAgentInstructionsApplicator(context, workspace.WorkspaceRoot);

        // Assert
        Assert.True(result);
        Assert.True(context.AgentInstructionsApplicatorAdded);
        Assert.Single(context.Applicators);
    }

    [Fact]
    public void TryAddAgentInstructionsApplicator_WhenAlreadyAdded_DoesNotAddAgainAndReturnsFalse()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        context.AgentInstructionsApplicatorAdded = true;

        // Act
        var result = CommonAgentApplicators.TryAddAgentInstructionsApplicator(context, workspace.WorkspaceRoot);

        // Assert
        Assert.False(result);
        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task TryAddAgentInstructionsApplicator_WhenAgentsMdExistsWithSameContent_DoesNotAddApplicator()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context1 = CreateScanContext(workspace.WorkspaceRoot);
        
        // First, create AGENTS.md by applying the applicator
        CommonAgentApplicators.TryAddAgentInstructionsApplicator(context1, workspace.WorkspaceRoot);
        await context1.Applicators[0].ApplyAsync(CancellationToken.None);
        
        var agentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.md");
        Assert.True(File.Exists(agentsFilePath));

        // Act - try to add the applicator again with a new context
        var context2 = CreateScanContext(workspace.WorkspaceRoot);
        var result = CommonAgentApplicators.TryAddAgentInstructionsApplicator(context2, workspace.WorkspaceRoot);

        // Assert - should not add applicator since AGENTS.md already exists with the same content
        Assert.False(result);
        Assert.True(context2.AgentInstructionsApplicatorAdded);
        Assert.Empty(context2.Applicators);
    }

    [Fact]
    public void TryAddAgentInstructionsApplicator_WhenAgentsMdExistsWithDifferentContent_AddsApplicatorForAspireFile()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        
        // Create AGENTS.md with different content
        var agentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.md");
        File.WriteAllText(agentsFilePath, "# Different Content\n\nThis is different.");

        // Act
        var result = CommonAgentApplicators.TryAddAgentInstructionsApplicator(context, workspace.WorkspaceRoot);

        // Assert
        Assert.True(result);
        Assert.True(context.AgentInstructionsApplicatorAdded);
        Assert.Single(context.Applicators);
    }

    [Fact]
    public void TryAddAgentInstructionsApplicator_WhenBothFilesExist_DoesNotAddApplicator()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        
        // Create both AGENTS.md and AGENTS.aspire.md
        var agentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.md");
        var aspireAgentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.aspire.md");
        File.WriteAllText(agentsFilePath, "# Different Content");
        File.WriteAllText(aspireAgentsFilePath, "# Aspire Content");

        // Act
        var result = CommonAgentApplicators.TryAddAgentInstructionsApplicator(context, workspace.WorkspaceRoot);

        // Assert
        Assert.False(result);
        Assert.True(context.AgentInstructionsApplicatorAdded);
        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task CreateAgentInstructionsAsync_CreatesAgentsMdWhenItDoesNotExist()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        // Act
        CommonAgentApplicators.TryAddAgentInstructionsApplicator(context, workspace.WorkspaceRoot);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        // Assert
        var agentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.md");
        Assert.True(File.Exists(agentsFilePath));
        var content = await File.ReadAllTextAsync(agentsFilePath);
        Assert.Contains("# Copilot instructions", content);
    }

    [Fact]
    public async Task CreateAgentInstructionsAsync_CreatesAspireAgentsMdWhenAgentsMdExists()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        
        // Create AGENTS.md with different content
        var agentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.md");
        File.WriteAllText(agentsFilePath, "# Existing Content");

        // Act
        CommonAgentApplicators.TryAddAgentInstructionsApplicator(context, workspace.WorkspaceRoot);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        // Assert
        var aspireAgentsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "AGENTS.aspire.md");
        Assert.True(File.Exists(aspireAgentsFilePath));
        var content = await File.ReadAllTextAsync(aspireAgentsFilePath);
        Assert.Contains("# Copilot instructions", content);
        
        // Original AGENTS.md should still have the original content
        var originalContent = await File.ReadAllTextAsync(agentsFilePath);
        Assert.Equal("# Existing Content", originalContent);
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
