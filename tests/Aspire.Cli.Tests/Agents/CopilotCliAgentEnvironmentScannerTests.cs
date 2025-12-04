// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents;
using Aspire.Cli.Agents.CopilotCli;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;

namespace Aspire.Cli.Tests.Agents;

public class CopilotCliAgentEnvironmentScannerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task ScanAsync_WhenCopilotCliInstalled_ReturnsApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var copilotCliRunner = new FakeCopilotCliRunner(new SemVersion(1, 0, 0));
        var scanner = new CopilotCliAgentEnvironmentScanner(copilotCliRunner, NullLogger<CopilotCliAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Single(context.Applicators);
        Assert.Contains("GitHub Copilot CLI", context.Applicators[0].Description);
    }

    [Fact]
    public async Task ScanAsync_WhenCopilotCliNotInstalled_ReturnsNoApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var copilotCliRunner = new FakeCopilotCliRunner(null);
        var scanner = new CopilotCliAgentEnvironmentScanner(copilotCliRunner, NullLogger<CopilotCliAgentEnvironmentScanner>.Instance);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Empty(context.Applicators);
    }

    [Fact]
    public async Task ApplyAsync_CreatesMcpConfigJsonWithCorrectConfiguration()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create a temporary .copilot folder in the workspace to avoid modifying the user's home directory
        var copilotFolder = workspace.CreateDirectory(".copilot");
        
        // Create a scanner that writes to a known test location
        var copilotCliRunner = new FakeCopilotCliRunner(new SemVersion(1, 0, 0));
        var scanner = new TestCopilotCliAgentEnvironmentScanner(copilotCliRunner, copilotFolder.FullName);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None);
        
        Assert.Single(context.Applicators);
        
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        var mcpConfigPath = Path.Combine(copilotFolder.FullName, "mcp-config.json");
        Assert.True(File.Exists(mcpConfigPath));

        var content = await File.ReadAllTextAsync(mcpConfigPath);
        var config = JsonNode.Parse(content)?.AsObject();
        Assert.NotNull(config);
        Assert.True(config.ContainsKey("mcpServers"));

        var servers = config["mcpServers"]?.AsObject();
        Assert.NotNull(servers);
        Assert.True(servers.ContainsKey("aspire"));

        var aspireServer = servers["aspire"]?.AsObject();
        Assert.NotNull(aspireServer);
        Assert.Equal("local", aspireServer["type"]?.GetValue<string>());
        Assert.Equal("aspire", aspireServer["command"]?.GetValue<string>());

        var args = aspireServer["args"]?.AsArray();
        Assert.NotNull(args);
        Assert.Equal(2, args.Count);
        Assert.Equal("mcp", args[0]?.GetValue<string>());
        Assert.Equal("start", args[1]?.GetValue<string>());

        // Verify env contains DOTNET_ROOT
        var env = aspireServer["env"]?.AsObject();
        Assert.NotNull(env);
        Assert.True(env.ContainsKey("DOTNET_ROOT"));
        Assert.Equal("${DOTNET_ROOT}", env["DOTNET_ROOT"]?.GetValue<string>());

        // Verify tools contains "*"
        var tools = aspireServer["tools"]?.AsArray();
        Assert.NotNull(tools);
        Assert.Single(tools);
        Assert.Equal("*", tools[0]?.GetValue<string>());
    }

    [Fact]
    public async Task ApplyAsync_PreservesExistingMcpConfigContent()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var copilotFolder = workspace.CreateDirectory(".copilot");
        
        // Create an existing mcp-config.json with another server
        var existingConfig = new JsonObject
        {
            ["mcpServers"] = new JsonObject
            {
                ["other-server"] = new JsonObject
                {
                    ["command"] = "other"
                }
            }
        };
        var mcpConfigPath = Path.Combine(copilotFolder.FullName, "mcp-config.json");
        await File.WriteAllTextAsync(mcpConfigPath, existingConfig.ToJsonString());

        var copilotCliRunner = new FakeCopilotCliRunner(new SemVersion(1, 0, 0));
        var scanner = new TestCopilotCliAgentEnvironmentScanner(copilotCliRunner, copilotFolder.FullName);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None);
        await context.Applicators[0].ApplyAsync(CancellationToken.None);

        var content = await File.ReadAllTextAsync(mcpConfigPath);
        var config = JsonNode.Parse(content)?.AsObject();
        Assert.NotNull(config);

        var servers = config["mcpServers"]?.AsObject();
        Assert.NotNull(servers);
        
        // Both servers should exist
        Assert.True(servers.ContainsKey("other-server"));
        Assert.True(servers.ContainsKey("aspire"));
    }

    [Fact]
    public async Task ScanAsync_WhenAspireAlreadyConfigured_ReturnsNoApplicator()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var copilotFolder = workspace.CreateDirectory(".copilot");
        
        // Create an existing mcp-config.json with aspire already configured
        var existingConfig = new JsonObject
        {
            ["mcpServers"] = new JsonObject
            {
                ["aspire"] = new JsonObject
                {
                    ["command"] = "aspire"
                }
            }
        };
        var mcpConfigPath = Path.Combine(copilotFolder.FullName, "mcp-config.json");
        await File.WriteAllTextAsync(mcpConfigPath, existingConfig.ToJsonString());

        var copilotCliRunner = new FakeCopilotCliRunner(new SemVersion(1, 0, 0));
        var scanner = new TestCopilotCliAgentEnvironmentScanner(copilotCliRunner, copilotFolder.FullName);
        var context = CreateScanContext(workspace.WorkspaceRoot);

        await scanner.ScanAsync(context, CancellationToken.None);

        Assert.Empty(context.Applicators);
    }

    private static AgentEnvironmentScanContext CreateScanContext(DirectoryInfo workingDirectory)
    {
        var executionContext = new CliExecutionContext(
            workingDirectory: workingDirectory,
            hivesDirectory: workingDirectory,
            cacheDirectory: workingDirectory,
            sdksDirectory: workingDirectory,
            debugMode: false,
            environmentVariables: null);

        return new AgentEnvironmentScanContext
        {
            WorkingDirectory = workingDirectory,
            RepositoryRoot = workingDirectory,
            ExecutionContext = executionContext
        };
    }

    /// <summary>
    /// A fake implementation of <see cref="ICopilotCliRunner"/> for testing.
    /// </summary>
    private sealed class FakeCopilotCliRunner(SemVersion? version) : ICopilotCliRunner
    {
        public Task<SemVersion?> GetVersionAsync(CancellationToken cancellationToken) => Task.FromResult(version);
    }

    /// <summary>
    /// A test implementation of the scanner that allows specifying a custom config directory.
    /// </summary>
    private sealed class TestCopilotCliAgentEnvironmentScanner : IAgentEnvironmentScanner
    {
        private const string McpConfigFileName = "mcp-config.json";
        private const string AspireServerName = "aspire";

        private readonly ICopilotCliRunner _copilotCliRunner;
        private readonly string _configDirectory;

        public TestCopilotCliAgentEnvironmentScanner(ICopilotCliRunner copilotCliRunner, string configDirectory)
        {
            _copilotCliRunner = copilotCliRunner;
            _configDirectory = configDirectory;
        }

        public async Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
        {
            var copilotVersion = await _copilotCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);

            if (copilotVersion is null)
            {
                return;
            }

            if (HasAspireServerConfigured())
            {
                return;
            }

            context.AddApplicator(CreateApplicator());
        }

        private string GetMcpConfigFilePath() => Path.Combine(_configDirectory, McpConfigFileName);

        private bool HasAspireServerConfigured()
        {
            var configFilePath = GetMcpConfigFilePath();

            if (!File.Exists(configFilePath))
            {
                return false;
            }

            try
            {
                var content = File.ReadAllText(configFilePath);
                var config = JsonNode.Parse(content)?.AsObject();

                if (config is null)
                {
                    return false;
                }

                if (config.TryGetPropertyValue("mcpServers", out var serversNode) && serversNode is JsonObject servers)
                {
                    return servers.ContainsKey(AspireServerName);
                }

                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private AgentEnvironmentApplicator CreateApplicator()
        {
            return new AgentEnvironmentApplicator(
                "Configure GitHub Copilot CLI to use the Aspire MCP server",
                ApplyMcpConfigurationAsync);
        }

        private async Task ApplyMcpConfigurationAsync(CancellationToken cancellationToken)
        {
            var configFilePath = GetMcpConfigFilePath();

            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            JsonObject config;

            if (File.Exists(configFilePath))
            {
                var existingContent = await File.ReadAllTextAsync(configFilePath, cancellationToken);
                config = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
            }
            else
            {
                config = new JsonObject();
            }

            if (!config.ContainsKey("mcpServers") || config["mcpServers"] is not JsonObject)
            {
                config["mcpServers"] = new JsonObject();
            }

            var servers = config["mcpServers"]!.AsObject();

            servers[AspireServerName] = new JsonObject
            {
                ["type"] = "local",
                ["command"] = "aspire",
                ["args"] = new JsonArray("mcp", "start"),
                ["env"] = new JsonObject
                {
                    ["DOTNET_ROOT"] = "${DOTNET_ROOT}"
                },
                ["tools"] = new JsonArray("*")
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(config, JsonSourceGenerationContext.Default.JsonObject);
            await File.WriteAllTextAsync(configFilePath, jsonContent, cancellationToken);
        }
    }
}
