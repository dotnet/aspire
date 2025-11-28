// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Git;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents.ClaudeCode;

/// <summary>
/// Scans for Claude Code environments and provides an applicator to configure the Aspire MCP server.
/// </summary>
internal sealed class ClaudeCodeAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    private const string ClaudeCodeFolderName = ".claude";
    private const string McpConfigFileName = ".mcp.json";
    private const string AspireServerName = "aspire";

    private readonly IGitRepository _gitRepository;
    private readonly IClaudeCodeCliRunner _claudeCodeCliRunner;

    /// <summary>
    /// Initializes a new instance of <see cref="ClaudeCodeAgentEnvironmentScanner"/>.
    /// </summary>
    /// <param name="gitRepository">The Git repository service for finding repository boundaries.</param>
    /// <param name="claudeCodeCliRunner">The Claude Code CLI runner for checking if Claude Code is installed.</param>
    public ClaudeCodeAgentEnvironmentScanner(IGitRepository gitRepository, IClaudeCodeCliRunner claudeCodeCliRunner)
    {
        ArgumentNullException.ThrowIfNull(gitRepository);
        ArgumentNullException.ThrowIfNull(claudeCodeCliRunner);
        _gitRepository = gitRepository;
        _claudeCodeCliRunner = claudeCodeCliRunner;
    }

    /// <inheritdoc />
    public async Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        // Get the git root to use as a boundary for searching
        var gitRoot = await _gitRepository.GetRootAsync(cancellationToken).ConfigureAwait(false);

        // Find the .claude folder to determine if Claude Code is being used in this project
        var claudeCodeFolder = FindClaudeCodeFolder(context.WorkingDirectory, gitRoot);

        // Determine the repo root - use git root, or infer from .claude folder location, or fall back to working directory
        DirectoryInfo? repoRoot = gitRoot;
        if (repoRoot is null && claudeCodeFolder is not null)
        {
            // .claude folder's parent is the repo root
            repoRoot = claudeCodeFolder.Parent;
        }

        if (claudeCodeFolder is not null || repoRoot is not null)
        {
            var targetRepoRoot = repoRoot ?? context.WorkingDirectory;

            // Check if the aspire server is already configured in .mcp.json
            if (HasAspireServerConfigured(targetRepoRoot))
            {
                // Already configured, no need to offer an applicator
                return;
            }

            // Found a .claude folder or git repo - add an applicator to configure MCP
            context.AddApplicator(CreateApplicator(targetRepoRoot));
        }
        else
        {
            // No .claude folder or git repo found - check if Claude Code CLI is installed
            var claudeCodeVersion = await _claudeCodeCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);

            if (claudeCodeVersion is not null)
            {
                // Claude Code is installed - offer to create config at working directory
                context.AddApplicator(CreateApplicator(context.WorkingDirectory));
            }
        }
    }

    /// <summary>
    /// Walks up the directory tree to find a .claude folder.
    /// Stops if we go above the git root (if provided).
    /// Ignores the .claude folder in the user's home directory.
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <param name="gitRoot">The git repository root, or null if not in a git repository.</param>
    private static DirectoryInfo? FindClaudeCodeFolder(DirectoryInfo startDirectory, DirectoryInfo? gitRoot)
    {
        var currentDirectory = startDirectory;
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        while (currentDirectory is not null)
        {
            // Check for .claude folder at current level, but ignore it if it's in the home directory
            // (the home directory's .claude folder is for user settings, not project config)
            var claudeCodePath = Path.Combine(currentDirectory.FullName, ClaudeCodeFolderName);
            if (Directory.Exists(claudeCodePath) && !string.Equals(currentDirectory.FullName, homeDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return new DirectoryInfo(claudeCodePath);
            }

            // Stop if we've reached the git root without finding .claude
            // (don't search above the repository boundary)
            if (gitRoot is not null && string.Equals(currentDirectory.FullName, gitRoot.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Checks if the repo root contains an .mcp.json file with an "aspire" MCP server configured.
    /// </summary>
    /// <param name="repoRoot">The repository root directory to check.</param>
    /// <returns>True if the aspire server is already configured, false otherwise.</returns>
    private static bool HasAspireServerConfigured(DirectoryInfo repoRoot)
    {
        var configFilePath = Path.Combine(repoRoot.FullName, McpConfigFileName);

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
    /// Creates an applicator for configuring the MCP server in the .mcp.json file at the repo root.
    /// </summary>
    private static AgentEnvironmentApplicator CreateApplicator(DirectoryInfo repoRoot)
    {
        return new AgentEnvironmentApplicator(
            ClaudeCodeAgentEnvironmentScannerStrings.ApplicatorDescription,
            async cancellationToken => await ApplyMcpConfigurationAsync(repoRoot, cancellationToken));
    }

    /// <summary>
    /// Creates or updates the .mcp.json file at the repo root.
    /// </summary>
    private static async Task ApplyMcpConfigurationAsync(DirectoryInfo repoRoot, CancellationToken cancellationToken)
    {
        var configFilePath = Path.Combine(repoRoot.FullName, McpConfigFileName);
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

        // Add or update the "aspire" server configuration
        servers[AspireServerName] = new JsonObject
        {
            ["command"] = "aspire",
            ["args"] = new JsonArray("mcp", "start")
        };

        // Write the updated config using AOT-compatible serialization
        var jsonContent = JsonSerializer.Serialize(config, JsonSourceGenerationContext.Default.JsonObject);
        await File.WriteAllTextAsync(configFilePath, jsonContent, cancellationToken);
    }
}
