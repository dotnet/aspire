// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Agents.CopilotCli;

/// <summary>
/// Scans for GitHub Copilot CLI environments and provides an applicator to configure the Aspire MCP server.
/// </summary>
internal sealed class CopilotCliAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    private const string CopilotFolderName = ".copilot";
    private const string McpConfigFileName = "mcp-config.json";
    private const string AspireServerName = "aspire";

    private readonly ICopilotCliRunner _copilotCliRunner;
    private readonly ILogger<CopilotCliAgentEnvironmentScanner> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CopilotCliAgentEnvironmentScanner"/>.
    /// </summary>
    /// <param name="copilotCliRunner">The Copilot CLI runner for checking if Copilot CLI is installed.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public CopilotCliAgentEnvironmentScanner(ICopilotCliRunner copilotCliRunner, ILogger<CopilotCliAgentEnvironmentScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(copilotCliRunner);
        ArgumentNullException.ThrowIfNull(logger);
        _copilotCliRunner = copilotCliRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting GitHub Copilot CLI environment scan");
        
        // Check if Copilot CLI is installed
        _logger.LogDebug("Checking for GitHub Copilot CLI installation...");
        var copilotVersion = await _copilotCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);

        if (copilotVersion is null)
        {
            _logger.LogDebug("GitHub Copilot CLI is not installed - skipping");
            // Copilot CLI is not installed, no need to offer configuration
            return;
        }

        _logger.LogDebug("Found GitHub Copilot CLI version: {Version}", copilotVersion);

        // Check if the aspire server is already configured in the global config
        _logger.LogDebug("Checking if Aspire MCP server is already configured in Copilot CLI global config...");
        if (HasAspireServerConfigured())
        {
            _logger.LogDebug("Aspire MCP server is already configured in Copilot CLI - skipping");
            // Already configured, no need to offer an applicator
            return;
        }

        // Copilot CLI is installed and aspire is not configured - offer to configure
        _logger.LogDebug("Adding Copilot CLI applicator for global MCP configuration");
        context.AddApplicator(CreateApplicator());
    }

    /// <summary>
    /// Gets the path to the Copilot CLI global configuration directory.
    /// </summary>
    private static string GetCopilotConfigDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, CopilotFolderName);
    }

    /// <summary>
    /// Gets the path to the Copilot CLI MCP configuration file.
    /// </summary>
    private static string GetMcpConfigFilePath()
    {
        return Path.Combine(GetCopilotConfigDirectory(), McpConfigFileName);
    }

    /// <summary>
    /// Checks if the Copilot CLI global configuration has an "aspire" MCP server configured.
    /// </summary>
    /// <returns>True if the aspire server is already configured, false otherwise.</returns>
    private static bool HasAspireServerConfigured()
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
            // If the JSON is malformed, assume aspire is not configured
            return false;
        }
    }

    /// <summary>
    /// Creates an applicator for configuring the MCP server in the Copilot CLI global configuration.
    /// </summary>
    private static AgentEnvironmentApplicator CreateApplicator()
    {
        return new AgentEnvironmentApplicator(
            CopilotCliAgentEnvironmentScannerStrings.ApplicatorDescription,
            ApplyMcpConfigurationAsync);
    }

    /// <summary>
    /// Creates or updates the mcp-config.json file in the Copilot CLI global configuration directory.
    /// </summary>
    private static async Task ApplyMcpConfigurationAsync(CancellationToken cancellationToken)
    {
        var configDirectory = GetCopilotConfigDirectory();
        var configFilePath = GetMcpConfigFilePath();

        // Ensure the .copilot directory exists
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }

        JsonObject config;

        // Read existing config or create new
        if (File.Exists(configFilePath))
        {
            var existingContent = await File.ReadAllTextAsync(configFilePath, cancellationToken);
            config = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
        }
        else
        {
            config = new JsonObject();
        }

        // Ensure "mcpServers" object exists
        if (!config.ContainsKey("mcpServers") || config["mcpServers"] is not JsonObject)
        {
            config["mcpServers"] = new JsonObject();
        }

        var servers = config["mcpServers"]!.AsObject();

        // Add or update the "aspire" server configuration with DOTNET_ROOT environment variable passthrough
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

        // Write the updated config using AOT-compatible serialization
        var jsonContent = JsonSerializer.Serialize(config, JsonSourceGenerationContext.Default.JsonObject);
        await File.WriteAllTextAsync(configFilePath, jsonContent, cancellationToken);
    }
}
