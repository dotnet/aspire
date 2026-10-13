// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;

namespace Aspire.Cli.Tests.Agents;

public class ClaudeCodeAgentEnvironmentScannerTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("{ invalid json content")]
    [InlineData("")]
    [InlineData("""{"mcpServers":{"aspire":{"command":"custom-aspire"}}}""")]
    public async Task ScanAsync_WithMcpConfiguration_DetectsInstalledCliWithoutModifyingFile(string content)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var configPath = Path.Combine(workspace.WorkspaceRoot.FullName, ".mcp.json");
        await File.WriteAllTextAsync(configPath, content);
        var runner = new FakeClaudeCodeCliRunner(new SemVersion(1, 0, 0));
        var scanner = new ClaudeCodeAgentEnvironmentScanner(runner,
            TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot),
            NullLogger<ClaudeCodeAgentEnvironmentScanner>.Instance);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workspace.WorkspaceRoot,
            RepositoryRoot = workspace.WorkspaceRoot
        };

        var environments = await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Equal(AgentEnvironment.ClaudeCode, environments);
        Assert.Equal(content, await File.ReadAllTextAsync(configPath));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("subdir")]
    public async Task ScanAsync_WithProjectDirectory_DetectsClaudeWithoutCallingRunner(string relativeWorkingDirectory)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        workspace.CreateDirectory(".claude");
        var workingDirectory = relativeWorkingDirectory == "." ? workspace.WorkspaceRoot : workspace.CreateDirectory(relativeWorkingDirectory);
        var homeDirectory = workspace.CreateDirectory("fake-home");
        var runner = new FakeClaudeCodeCliRunner(null);
        var scanner = new ClaudeCodeAgentEnvironmentScanner(runner,
            TestExecutionContextHelper.CreateExecutionContext(workingDirectory, homeDirectory: homeDirectory),
            NullLogger<ClaudeCodeAgentEnvironmentScanner>.Instance);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = workingDirectory,
            RepositoryRoot = workspace.WorkspaceRoot
        };
        var before = Directory.GetFileSystemEntries(workspace.WorkspaceRoot.FullName, "*", SearchOption.AllDirectories);

        var environments = await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Equal(AgentEnvironment.ClaudeCode, environments);
        Assert.False(runner.WasCalled);
        Assert.Equal(before, Directory.GetFileSystemEntries(workspace.WorkspaceRoot.FullName, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ScanAsync_IgnoresHomeAndDirectoriesAboveRepositoryRoot(bool directoryIsHome)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        workspace.CreateDirectory(".claude");
        var childDirectory = workspace.CreateDirectory("subdir");
        var homeDirectory = directoryIsHome ? workspace.WorkspaceRoot : workspace.CreateDirectory("fake-home");
        var runner = new FakeClaudeCodeCliRunner(null);
        var scanner = new ClaudeCodeAgentEnvironmentScanner(runner,
            TestExecutionContextHelper.CreateExecutionContext(childDirectory, homeDirectory: homeDirectory),
            NullLogger<ClaudeCodeAgentEnvironmentScanner>.Instance);
        var context = new AgentEnvironmentScanContext
        {
            WorkingDirectory = childDirectory,
            RepositoryRoot = directoryIsHome ? workspace.WorkspaceRoot : childDirectory
        };

        var environments = await scanner.ScanAsync(context, CancellationToken.None).DefaultTimeout();

        Assert.Equal(AgentEnvironment.None, environments);
        Assert.True(runner.WasCalled);
    }

    private sealed class FakeClaudeCodeCliRunner(SemVersion? version) : IClaudeCodeCliRunner
    {
        public bool WasCalled { get; private set; }

        public Task<SemVersion?> GetVersionAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;

            return Task.FromResult(version);
        }
    }
}
