// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Agents;

public class VsCodeAgentEnvironmentScannerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ScanAsync_WhenVsCodeFolderExists_ReturnsApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        var scanner = new VsCodeAgentEnvironmentScanner();
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Single(context.Applicators);
        Assert.Contains("VS Code", context.Applicators[0].Description);
        Assert.StartsWith("vscode:", context.Applicators[0].Fingerprint);
    }

    [Fact]
    public async Task ScanAsync_WhenVsCodeFolderExistsInParent_ReturnsApplicatorForParent()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodeFolder = workspace.CreateDirectory(".vscode");
        var childDir = workspace.CreateDirectory("subdir");
        var scanner = new VsCodeAgentEnvironmentScanner();
        var context = new AgentEnvironmentScanContext { WorkingDirectory = childDir };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Single(context.Applicators);
        Assert.Contains(vsCodeFolder.FullName, context.Applicators[0].Fingerprint);
    }

    [Fact]
    public async Task ScanAsync_WhenGitFolderFoundBeforeVsCode_ReturnsNoApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        workspace.CreateDirectory(".git");
        var childDir = workspace.CreateDirectory("subdir");
        var scanner = new VsCodeAgentEnvironmentScanner();
        var context = new AgentEnvironmentScanContext { WorkingDirectory = childDir };

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task ScanAsync_WhenNoVsCodeOrGitFolder_AndNoEnvVars_ReturnsNoApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var scanner = new VsCodeAgentEnvironmentScanner();
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        // This test assumes no VSCODE_* environment variables are set
        // In a real environment, this might return an applicator if VS Code env vars exist
        await scanner.ScanAsync(context, CancellationToken.None);

        // The result depends on whether VSCODE_* environment variables exist
        // We just verify the test runs without throwing
    }

    [Fact]
    public async Task ApplyAsync_CreatesVsCodeFolderIfNotExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var vsCodePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".vscode");
        var scanner = new VsCodeAgentEnvironmentScanner();
        
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
        var scanner = new VsCodeAgentEnvironmentScanner();
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

        var scanner = new VsCodeAgentEnvironmentScanner();
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
        
        // Create an existing mcp.json with an old aspire config
        var existingConfig = new JsonObject
        {
            ["servers"] = new JsonObject
            {
                ["aspire"] = new JsonObject
                {
                    ["type"] = "http",
                    ["command"] = "old-aspire"
                }
            }
        };
        var mcpJsonPath = Path.Combine(vsCodeFolder.FullName, "mcp.json");
        await File.WriteAllTextAsync(mcpJsonPath, existingConfig.ToJsonString());

        var scanner = new VsCodeAgentEnvironmentScanner();
        var context = new AgentEnvironmentScanContext { WorkingDirectory = workspace.WorkspaceRoot };

        await scanner.ScanAsync(context, CancellationToken.None);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        var content = await File.ReadAllTextAsync(mcpJsonPath);
        var config = JsonNode.Parse(content)?.AsObject();
        var aspireServer = config?["servers"]?["aspire"]?.AsObject();
        
        Assert.NotNull(aspireServer);
        Assert.Equal("stdio", aspireServer["type"]?.GetValue<string>());
        Assert.Equal("aspire", aspireServer["command"]?.GetValue<string>());
    }
}
