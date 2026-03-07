// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class StandardLocationAgentEnvironmentScannerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ScanAsync_AddsWorkspaceAndUserSkillApplicators()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var scanner = new StandardLocationAgentEnvironmentScanner(executionContext, NullLogger<StandardLocationAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Equal(4, context.Applicators.Count);
        Assert.Contains(context.Applicators, a => a.Description.Contains(".agents/skills/aspire"));
        Assert.Contains(context.Applicators, a => a.Description.Contains(".agents/skills/dotnet-inspect"));
        Assert.Contains(context.Applicators, a => a.Description.Contains("~/.agents/skills/aspire"));
        Assert.Contains(context.Applicators, a => a.Description.Contains("~/.agents/skills/dotnet-inspect"));
    }

    [Fact]
    public async Task ScanAsync_CalledTwice_DoesNotDuplicate()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var scanner = new StandardLocationAgentEnvironmentScanner(executionContext, NullLogger<StandardLocationAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();
        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Equal(4, context.Applicators.Count);
    }

    [Fact]
    public async Task ScanAsync_CreatesFilesInBothLocations()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        using var homeDir = TemporaryWorkspace.Create(outputHelper);
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot, homeDir.WorkspaceRoot);
        var scanner = new StandardLocationAgentEnvironmentScanner(executionContext, NullLogger<StandardLocationAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        foreach (var applicator in context.Applicators)
        {
            await applicator.ApplyAsync(CancellationToken.None).DefaultTimeout();
        }

        // Verify workspace-level files
        var wsAspire = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "aspire", "SKILL.md");
        var wsDotnetInspect = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "dotnet-inspect", "SKILL.md");
        Assert.True(File.Exists(wsAspire));
        Assert.True(File.Exists(wsDotnetInspect));
        Assert.Contains("# Aspire Skill", await File.ReadAllTextAsync(wsAspire));
        Assert.Contains("# dotnet-inspect", await File.ReadAllTextAsync(wsDotnetInspect));

        // Verify user-level files
        var userAspire = Path.Combine(homeDir.WorkspaceRoot.FullName, ".agents", "skills", "aspire", "SKILL.md");
        var userDotnetInspect = Path.Combine(homeDir.WorkspaceRoot.FullName, ".agents", "skills", "dotnet-inspect", "SKILL.md");
        Assert.True(File.Exists(userAspire));
        Assert.True(File.Exists(userDotnetInspect));
        Assert.Contains("# Aspire Skill", await File.ReadAllTextAsync(userAspire));
        Assert.Contains("# dotnet-inspect", await File.ReadAllTextAsync(userDotnetInspect));
    }

    [Fact]
    public async Task ScanAsync_WhenWorkspaceFilesAlreadyCurrent_OnlyAddsUserLevelApplicators()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        using var homeDir = TemporaryWorkspace.Create(outputHelper);

        // Pre-create workspace-level skill files with current content
        var wsAspirePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "aspire", "SKILL.md");
        var wsDotnetInspectPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "dotnet-inspect", "SKILL.md");
        Directory.CreateDirectory(Path.GetDirectoryName(wsAspirePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(wsDotnetInspectPath)!);
        await File.WriteAllTextAsync(wsAspirePath, CommonAgentApplicators.SkillFileContent);
        await File.WriteAllTextAsync(wsDotnetInspectPath, CommonAgentApplicators.DotnetInspectSkillFileContent);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot, homeDir.WorkspaceRoot);
        var scanner = new StandardLocationAgentEnvironmentScanner(executionContext, NullLogger<StandardLocationAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        // Only user-level applicators should be present (workspace files are current)
        Assert.Equal(2, context.Applicators.Count);
        Assert.Contains(context.Applicators, a => a.Description.Contains("~/.agents/skills/aspire"));
        Assert.Contains(context.Applicators, a => a.Description.Contains("~/.agents/skills/dotnet-inspect"));
    }

    [Fact]
    public async Task ScanAsync_WhenExistingFilesHaveStaleContent_AddsUpdateApplicators()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        using var homeDir = TemporaryWorkspace.Create(outputHelper);

        // Pre-create workspace-level skill file with outdated content
        var wsAspirePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".agents", "skills", "aspire", "SKILL.md");
        Directory.CreateDirectory(Path.GetDirectoryName(wsAspirePath)!);
        await File.WriteAllTextAsync(wsAspirePath, "# Old Aspire Skill\n\nOutdated.");

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot, homeDir.WorkspaceRoot);
        var scanner = new StandardLocationAgentEnvironmentScanner(executionContext, NullLogger<StandardLocationAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        // Workspace aspire should be an update, the rest should be creates
        Assert.Equal(4, context.Applicators.Count);
        Assert.Contains(context.Applicators, a => a.Description.Contains(".agents/skills/aspire") && a.Description.Contains("update"));
    }

    [Fact]
    public async Task ScanAsync_RegistersStandardSkillBaseDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var scanner = new StandardLocationAgentEnvironmentScanner(executionContext, NullLogger<StandardLocationAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Contains(Path.Combine(".agents", "skills"), context.SkillBaseDirectories);
    }

    private static CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory, DirectoryInfo? homeDirectory = null)
    {
        return new CliExecutionContext(
            workingDirectory: workingDirectory,
            hivesDirectory: workingDirectory,
            cacheDirectory: workingDirectory,
            sdksDirectory: workingDirectory,
            logsDirectory: workingDirectory,
            logFilePath: "test.log",
            debugMode: false,
            environmentVariables: new Dictionary<string, string?>(),
            homeDirectory: homeDirectory ?? workingDirectory);
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
