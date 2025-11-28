// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Git;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents.VsCode;

/// <summary>
/// Scans for VS Code environments and provides an applicator to configure the Aspire MCP server.
/// Also covers Copilot CLI since VS Code and Copilot share a configuration file.
/// </summary>
internal sealed class VsCodeAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    private const string VsCodeFolderName = ".vscode";
    private const string McpConfigFileName = "mcp.json";
    private const string VsCodeEnvironmentVariablePrefix = "VSCODE_";
    private const string AspireServerName = "aspire";

    private readonly IGitRepository _gitRepository;
    private readonly IVsCodeCliRunner _vsCodeCliRunner;
    private readonly ICopilotCliRunner _copilotCliRunner;

    /// <summary>
    /// Initializes a new instance of <see cref="VsCodeAgentEnvironmentScanner"/>.
    /// </summary>
    /// <param name="gitRepository">The Git repository service for finding repository boundaries.</param>
    /// <param name="vsCodeCliRunner">The VS Code CLI runner for checking if VS Code is installed.</param>
    /// <param name="copilotCliRunner">The Copilot CLI runner for checking if Copilot CLI is installed.</param>
    public VsCodeAgentEnvironmentScanner(IGitRepository gitRepository, IVsCodeCliRunner vsCodeCliRunner, ICopilotCliRunner copilotCliRunner)
    {
        ArgumentNullException.ThrowIfNull(gitRepository);
        ArgumentNullException.ThrowIfNull(vsCodeCliRunner);
        ArgumentNullException.ThrowIfNull(copilotCliRunner);
        _gitRepository = gitRepository;
        _vsCodeCliRunner = vsCodeCliRunner;
        _copilotCliRunner = copilotCliRunner;
    }

    /// <inheritdoc />
    public async Task ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        // Get the git root to use as a boundary for searching
        var gitRoot = await _gitRepository.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var vsCodeFolder = FindVsCodeFolder(context.WorkingDirectory, gitRoot);

        if (vsCodeFolder is not null)
        {
            // Check if the aspire server is already configured
            if (HasAspireServerConfigured(vsCodeFolder))
            {
                // Already configured, no need to offer an applicator
                return;
            }

            // Found a .vscode folder - add an applicator to configure MCP
            context.AddApplicator(CreateApplicator(vsCodeFolder));
        }
        else if (await IsVsCodeOrCopilotAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            // No .vscode folder found, but VS Code or Copilot CLI is available
            // Use git root if available, otherwise fall back to current working directory
            var targetDirectory = gitRoot ?? context.WorkingDirectory;
            var targetVsCodeFolder = new DirectoryInfo(Path.Combine(targetDirectory.FullName, VsCodeFolderName));
            context.AddApplicator(CreateApplicator(targetVsCodeFolder));
        }
    }

    /// <summary>
    /// Checks if VS Code or Copilot CLI is available on the machine.
    /// First checks for VS Code environment variables (low cost),
    /// then falls back to checking for the CLI executables.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if VS Code or Copilot CLI is available, false otherwise.</returns>
    private async Task<bool> IsVsCodeOrCopilotAvailableAsync(CancellationToken cancellationToken)
    {
        // First check environment variables (low cost)
        if (HasVsCodeEnvironmentVariables())
        {
            return true;
        }

        // Try VS Code stable
        var vsCodeVersion = await _vsCodeCliRunner.GetVersionAsync(new VsCodeRunOptions { UseInsiders = false }, cancellationToken).ConfigureAwait(false);
        if (vsCodeVersion is not null)
        {
            return true;
        }

        // Try VS Code Insiders
        var vsCodeInsidersVersion = await _vsCodeCliRunner.GetVersionAsync(new VsCodeRunOptions { UseInsiders = true }, cancellationToken).ConfigureAwait(false);
        if (vsCodeInsidersVersion is not null)
        {
            return true;
        }

        // Try Copilot CLI (shares configuration file with VS Code)
        var copilotVersion = await _copilotCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        if (copilotVersion is not null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Walks up the directory tree to find a .vscode folder.
    /// Stops if we go above the git root (if provided).
    /// Ignores the .vscode folder in the user's home directory (used for user settings, not workspace config).
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <param name="gitRoot">The git repository root, or null if not in a git repository.</param>
    private static DirectoryInfo? FindVsCodeFolder(DirectoryInfo startDirectory, DirectoryInfo? gitRoot)
    {
        var currentDirectory = startDirectory;
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        while (currentDirectory is not null)
        {
            // Check for .vscode folder at current level, but ignore it if it's in the home directory
            // (the home directory's .vscode folder is for user settings, not workspace config)
            var vsCodePath = Path.Combine(currentDirectory.FullName, VsCodeFolderName);
            if (Directory.Exists(vsCodePath) && !string.Equals(currentDirectory.FullName, homeDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return new DirectoryInfo(vsCodePath);
            }

            // Stop if we've reached the git root without finding .vscode
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
    /// Checks if the .vscode folder contains an mcp.json file with an "aspire" server configured.
    /// </summary>
    /// <param name="vsCodeFolder">The .vscode folder to check.</param>
    /// <returns>True if the aspire server is already configured, false otherwise.</returns>
    private static bool HasAspireServerConfigured(DirectoryInfo vsCodeFolder)
    {
        var mcpConfigPath = Path.Combine(vsCodeFolder.FullName, McpConfigFileName);

        if (!File.Exists(mcpConfigPath))
        {
            return false;
        }

        try
        {
            var content = File.ReadAllText(mcpConfigPath);
            var config = JsonNode.Parse(content)?.AsObject();

            if (config is null)
            {
                return false;
            }

            if (config.TryGetPropertyValue("servers", out var serversNode) && serversNode is JsonObject servers)
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
    /// Creates an applicator for configuring the MCP server in the specified .vscode folder.
    /// </summary>
    private static AgentEnvironmentApplicator CreateApplicator(DirectoryInfo vsCodeFolder)
    {
        var mcpConfigPath = Path.Combine(vsCodeFolder.FullName, McpConfigFileName);
        var fingerprint = CreateFingerprint("vscode", mcpConfigPath);

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

    /// <summary>
    /// Creates a deterministic fingerprint hash from an agent type and path.
    /// </summary>
    private static string CreateFingerprint(string agentType, string path)
    {
        var input = $"{agentType}:{path}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Use first 16 characters of the hex string (8 bytes) for a shorter but still unique fingerprint
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }
}
