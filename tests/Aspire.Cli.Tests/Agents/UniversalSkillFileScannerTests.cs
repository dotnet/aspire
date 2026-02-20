// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class UniversalSkillFileScannerTests(ITestOutputHelper outputHelper)
{
    private static readonly string s_universalSkillFilePath = Path.Combine(".agents", "skills", "aspire", "SKILL.md");

    [Fact]
    public async Task ScanAsync_AlwaysAddsUniversalSkillFileApplicator()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        var scanner = new UniversalSkillFileScanner(NullLogger<UniversalSkillFileScanner>.Instance);

        // Act
        await scanner.ScanAsync(context, CancellationToken.None);

        // Assert
        Assert.True(context.HasSkillFileApplicator(s_universalSkillFilePath));
        Assert.Single(context.Applicators);
        Assert.Contains("SKILL.md", context.Applicators[0].Description);
    }

    [Fact]
    public async Task ScanAsync_WhenSkillFileAlreadyExists_DoesNotAddApplicator()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        var scanner = new UniversalSkillFileScanner(NullLogger<UniversalSkillFileScanner>.Instance);
        
        // Create the skill file with current content
        var skillFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, s_universalSkillFilePath);
        var skillDirectory = Path.GetDirectoryName(skillFilePath)!;
        Directory.CreateDirectory(skillDirectory);
        File.WriteAllText(skillFilePath, CommonAgentApplicators.SkillFileContent);

        // Act
        await scanner.ScanAsync(context, CancellationToken.None);

        // Assert - should not add applicator since skill file already exists with same content
        Assert.True(context.HasSkillFileApplicator(s_universalSkillFilePath));
        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task ScanAsync_WhenSkillFileHasOutdatedContent_AddsUpdateApplicator()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var context = CreateScanContext(workspace.WorkspaceRoot);
        var scanner = new UniversalSkillFileScanner(NullLogger<UniversalSkillFileScanner>.Instance);
        
        // Create the skill file with outdated content
        var skillFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, s_universalSkillFilePath);
        var skillDirectory = Path.GetDirectoryName(skillFilePath)!;
        Directory.CreateDirectory(skillDirectory);
        File.WriteAllText(skillFilePath, "# Old Aspire Skill\n\nOutdated content.");

        // Act
        await scanner.ScanAsync(context, CancellationToken.None);

        // Assert - should add update applicator
        Assert.True(context.HasSkillFileApplicator(s_universalSkillFilePath));
        Assert.Single(context.Applicators);
        Assert.Contains("update", context.Applicators[0].Description);
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
