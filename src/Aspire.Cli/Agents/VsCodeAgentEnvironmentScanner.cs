// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// Scans for VS Code environments and provides an applicator to configure the Aspire MCP server.
/// </summary>
internal sealed class VsCodeAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    private const string VsCodeFolderName = ".vscode";
    private const string GitFolderName = ".git";
    private const string McpConfigFileName = "mcp.json";
    private const string VsCodeEnvironmentVariablePrefix = "VSCODE_";

    /// <inheritdoc />
    public Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        var vsCodeFolder = FindVsCodeFolder(context.WorkingDirectory);

        if (vsCodeFolder is not null)
        {
            // Found a .vscode folder - add an applicator to configure MCP
            context.AddApplicator(CreateApplicator(vsCodeFolder));
        }
        else if (HasVsCodeEnvironmentVariables())
        {
            // No .vscode folder found, but VS Code environment variables are present
            // Create config in the current working directory
            var targetVsCodeFolder = new DirectoryInfo(Path.Combine(context.WorkingDirectory.FullName, VsCodeFolderName));
            context.AddApplicator(CreateApplicator(targetVsCodeFolder));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Walks up the directory tree to find a .vscode folder.
    /// Stops if a .git folder is found (indicating repo root).
    /// </summary>
    private static DirectoryInfo? FindVsCodeFolder(DirectoryInfo startDirectory)
    {
        var currentDirectory = startDirectory;

        while (currentDirectory is not null)
        {
            // Check for .vscode folder at current level
            var vsCodePath = Path.Combine(currentDirectory.FullName, VsCodeFolderName);
            if (Directory.Exists(vsCodePath))
            {
                return new DirectoryInfo(vsCodePath);
            }

            // Check for .git folder - if found, we've reached repo root without finding .vscode
            var gitPath = Path.Combine(currentDirectory.FullName, GitFolderName);
            if (Directory.Exists(gitPath))
            {
                return null;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Checks if any VS Code environment variables are present.
    /// </summary>
    private static bool HasVsCodeEnvironmentVariables()
    {
        var environmentVariables = Environment.GetEnvironmentVariables();
        foreach (var key in environmentVariables.Keys)
        {
            if (key is string keyString && keyString.StartsWith(VsCodeEnvironmentVariablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Creates an applicator for configuring the MCP server in the specified .vscode folder.
    /// </summary>
    private static AgentEnvironmentApplicator CreateApplicator(DirectoryInfo vsCodeFolder)
    {
        var mcpConfigPath = Path.Combine(vsCodeFolder.FullName, McpConfigFileName);
        var fingerprint = $"vscode:{mcpConfigPath}";

        return new AgentEnvironmentApplicator(
            VsCodeAgentEnvironmentScannerStrings.ApplicatorDescription,
            fingerprint,
            async cancellationToken => await ApplyMcpConfigurationAsync(vsCodeFolder, cancellationToken));
    }

    /// <summary>
    /// Creates or updates the mcp.json file in the .vscode folder.
    /// </summary>
    private static async Task ApplyMcpConfigurationAsync(DirectoryInfo vsCodeFolder, CancellationToken cancellationToken)
    {
        // Ensure the .vscode folder exists
        if (!vsCodeFolder.Exists)
        {
            vsCodeFolder.Create();
        }

        var mcpConfigPath = Path.Combine(vsCodeFolder.FullName, McpConfigFileName);
        JsonObject config;

        // Read existing config or create new
        if (File.Exists(mcpConfigPath))
        {
            var existingContent = await File.ReadAllTextAsync(mcpConfigPath, cancellationToken);
            config = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
        }
        else
        {
            config = new JsonObject();
        }

        // Ensure "servers" object exists
        if (!config.ContainsKey("servers") || config["servers"] is not JsonObject)
        {
            config["servers"] = new JsonObject();
        }

        var servers = config["servers"]!.AsObject();

        // Add or update the "aspire" server configuration
        servers["aspire"] = new JsonObject
        {
            ["type"] = "stdio",
            ["command"] = "aspire",
            ["args"] = new JsonArray("mcp", "start"),
            ["tools"] = new JsonArray("*")
        };

        // Write the updated config with indentation using AOT-compatible serialization
        var jsonContent = JsonSerializer.Serialize(config, JsonSourceGenerationContext.Default.JsonObject);
        await File.WriteAllTextAsync(mcpConfigPath, jsonContent, cancellationToken);
    }
}
