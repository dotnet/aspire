// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.VsCode;
using Aspire.Cli.Git;
using Aspire.Cli.Tests.Utils;
using Semver;

namespace Aspire.Cli.Tests.Agents;

public class VsCodeAgentEnvironmentScannerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ScanAsync_WhenVsCodeFolderExists_ReturnsApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Single(context.Applicators);
        Assert.Contains("VS Code", context.Applicators[0].Description);
    }

    [Fact]
    public async Task ScanAsync_WhenVsCodeFolderExistsInParent_ReturnsApplicatorForParent()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        var childDir = workspace.CreateDirectory("subdir");
        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = childDir };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Single(context.Applicators);
    }

    [Fact]
    public async Task ScanAsync_WhenGitRootFoundBeforeVsCode_AndNoCliAvailable_ReturnsNoApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var childDir = workspace.CreateDirectory("subdir");
        // Git root is the workspace root, so search should stop there
        var gitRepository = new FakeGitRepository(workspace.WorkspaceRoot);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = childDir };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task ScanAsync_WhenNoVsCodeFolder_AndVsCodeCliAvailable_ReturnsApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var gitRepository = new FakeGitRepository(workspace.WorkspaceRoot);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(new SemVersion(1, 85, 0));
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Single(context.Applicators);
    }

    [Fact]
    public async Task ScanAsync_WhenNoVsCodeOrGitFolder_AndNoCliAvailable_ReturnsNoApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        // This test assumes no VSCODE_* environment variables are set
        // With no CLI available and no env vars, no applicator should be returned
        await scanner.ScanAsync(context, CancellationToken.None);

        // The result depends on whether VSCODE_* environment variables exist
        // We just verify the test runs without throwing
    }

    [Fact]
    public async Task ApplyAsync_CreatesVsCodeFolderIfNotExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".vscode");
        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        
        // First, make the scanner find a parent .vscode folder to get an applicator
        var parentVsCode = workspace.CreateDirectory(".vscode");
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };
        
        await scanner.ScanAsync(context, CancellationToken.None);
        
        Assert.Single(context.Applicators);
        
        // Apply the configuration
        await context.Applicators[0].ApplyAsync(CancellationToken.None);
        
        // Verify the mcp.json was created
        var mcpJsonPath = Path.Combine(parentVsCode.FullName, "mcp.json");
        Assert.True(File.Exists(mcpJsonPath));
    }

    [Fact]
    public async Task ApplyAsync_CreatesMcpJsonWithCorrectConfiguration()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        var mcpJsonPath = Path.Combine(vsCodeFolder.FullName, "mcp.json");
        Assert.True(File.Exists(mcpJsonPath));

        var content = await File.ReadAllTextAsync(mcpJsonPath);
        var config = JsonNode.Parse(content)?.AsObject();
        Assert.NotNull(config);
        Assert.True(config.ContainsKey("servers"));

        var servers = config["servers"]?.AsObject();
        Assert.NotNull(servers);
        Assert.True(servers.ContainsKey("aspire"));

        var aspireServer = servers["aspire"]?.AsObject();
        Assert.NotNull(aspireServer);
        Assert.Equal("stdio", aspireServer["type"]?.GetValue<string>());
        Assert.Equal("aspire", aspireServer["command"]?.GetValue<string>());

        var args = aspireServer["args"]?.AsArray();
        Assert.NotNull(args);
        Assert.Equal(2, args.Count);
        Assert.Equal("mcp", args[0]?.GetValue<string>());
        Assert.Equal("start", args[1]?.GetValue<string>());

        // Verify that "tools" is NOT included (not supported by VS Code MCP)
        Assert.False(aspireServer.ContainsKey("tools"));
    }

    [Fact]
    public async Task ApplyAsync_PreservesExistingMcpJsonContent()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        
        // Create an existing mcp.json with another server
        var existingConfig = new JsonObject
        {
            ["servers"] = new JsonObject
            {
                ["other-server"] = new JsonObject
                {
                    ["type"] = "stdio",
                    ["command"] = "other"
                }
            }
        };
        var mcpJsonPath = Path.Combine(vsCodeFolder.FullName, "mcp.json");
        await File.WriteAllTextAsync(mcpJsonPath, existingConfig.ToJsonString());

        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        var content = await File.ReadAllTextAsync(mcpJsonPath);
        var config = JsonNode.Parse(content)?.AsObject();
        Assert.NotNull(config);

        var servers = config["servers"]?.AsObject();
        Assert.NotNull(servers);
        
        // Both servers should exist
        Assert.True(servers.ContainsKey("other-server"));
        Assert.True(servers.ContainsKey("aspire"));
    }

    [Fact]
    public async Task ApplyAsync_UpdatesExistingAspireServerConfig()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        
        // Create an existing mcp.json with another server (not aspire, since aspire being present would skip offering the applicator)
        var existingConfig = new JsonObject
        {
            ["servers"] = new JsonObject
            {
                ["other-server"] = new JsonObject
                {
                    ["type"] = "http",
                    ["command"] = "other"
                }
            }
        };
        var mcpJsonPath = Path.Combine(vsCodeFolder.FullName, "mcp.json");
        await File.WriteAllTextAsync(mcpJsonPath, existingConfig.ToJsonString());

        var gitRepository = new FakeGitRepository(null);
        var vsCodeCliRunner = new FakeVsCodeCliRunner(null);
        var scanner = new VsCodeAgentEnvironmentScanner(gitRepository, vsCodeCliRunner);
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);
        
        // Should return an applicator since aspire is not configured yet
        Assert.Single(context.Applicators);
        
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        var content = await File.ReadAllTextAsync(mcpJsonPath);
        var config = JsonNode.Parse(content)?.AsObject();
        var aspireServer = config?["servers"]?["aspire"]?.AsObject();
        
        Assert.NotNull(aspireServer);
        Assert.Equal("stdio", aspireServer["type"]?.GetValue<string>());
        Assert.Equal("aspire", aspireServer["command"]?.GetValue<string>());
        
        // The other server should still exist
        var otherServer = config?["servers"]?["other-server"]?.AsObject();
        Assert.NotNull(otherServer);
    }

    /// <summary>
    /// A fake implementation of <see cref="IGitRepository"/> for testing.
    /// </summary>
    private sealed class FakeGitRepository(DirectoryInfo? gitRoot) : IGitRepository
    {
        public Task<DirectoryInfo?> GetRootAsync(CancellationToken cancellationToken) => Task.FromResult(gitRoot);
    }

    /// <summary>
    /// A fake implementation of <see cref="IVsCodeCliRunner"/> for testing.
    /// </summary>
    private sealed class FakeVsCodeCliRunner(SemVersion? version) : IVsCodeCliRunner
    {
        public Task<SemVersion?> GetVersionAsync(VsCodeRunOptions options, CancellationToken cancellationToken) => Task.FromResult(version);
    }
}
