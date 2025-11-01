// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.CodingAgent;

/// <summary>
/// Interface for configuring workspaces for use with Copilot and coding agents.
/// </summary>
internal interface ICodingAgentConfigurator
{
    /// <summary>
    /// Configures the workspace with coding agent optimization files.
    /// </summary>
    /// <param name="workspacePath">The path to the workspace (Git root or target directory).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if configuration was successful; otherwise, false.</returns>
    Task<bool> ConfigureWorkspaceAsync(DirectoryInfo workspacePath, CancellationToken cancellationToken);
}

/// <summary>
/// Configures workspaces for use with Copilot and coding agents.
/// </summary>
internal sealed class CodingAgentConfigurator(ILogger<CodingAgentConfigurator> logger) : ICodingAgentConfigurator
{
    private const string CopilotInstructionsFileName = ".github/copilot-instructions.md";
    private const string McpSettingsFileName = ".mcp/settings.json";

    /// <inheritdoc/>
    public async Task<bool> ConfigureWorkspaceAsync(DirectoryInfo workspacePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspacePath);

        if (!workspacePath.Exists)
        {
            logger.LogWarning("Workspace path does not exist: {WorkspacePath}", workspacePath.FullName);
            return false;
        }

        logger.LogDebug("Configuring workspace for coding agents at: {WorkspacePath}", workspacePath.FullName);

        try
        {
            // Create Copilot instructions file if it doesn't exist
            await CreateCopilotInstructionsAsync(workspacePath, cancellationToken);

            // Check for and configure MCP settings if the file exists
            await ConfigureMcpSettingsAsync(workspacePath, cancellationToken);

            logger.LogInformation("Successfully configured workspace for coding agents");
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to configure workspace for coding agents");
            return false;
        }
    }

    private async Task CreateCopilotInstructionsAsync(DirectoryInfo workspacePath, CancellationToken cancellationToken)
    {
        var copilotInstructionsPath = Path.Combine(workspacePath.FullName, CopilotInstructionsFileName);
        var copilotInstructionsFile = new FileInfo(copilotInstructionsPath);

        if (copilotInstructionsFile.Exists)
        {
            logger.LogDebug("Copilot instructions file already exists at: {Path}", copilotInstructionsPath);
            return;
        }

        // Create .github directory if it doesn't exist
        var githubDir = Path.Combine(workspacePath.FullName, ".github");
        Directory.CreateDirectory(githubDir);

        // Create basic Copilot instructions for Aspire projects
        var instructionsContent = new StringBuilder();
        instructionsContent.AppendLine("# Aspire Project Instructions");
        instructionsContent.AppendLine();
        instructionsContent.AppendLine("This is an Aspire project. When working with this codebase:");
        instructionsContent.AppendLine();
        instructionsContent.AppendLine("## Key Technologies");
        instructionsContent.AppendLine("- .NET Aspire: Cloud-native distributed application framework");
        instructionsContent.AppendLine("- C# and .NET");
        instructionsContent.AppendLine();
        instructionsContent.AppendLine("## Project Structure");
        instructionsContent.AppendLine("- **AppHost**: Orchestration project that defines the application model");
        instructionsContent.AppendLine("- **ServiceDefaults**: Shared configuration and telemetry setup");
        instructionsContent.AppendLine("- Service projects: Individual microservices or applications");
        instructionsContent.AppendLine();
        instructionsContent.AppendLine("## Development Workflow");
        instructionsContent.AppendLine("- Use `dotnet run` in the AppHost project to start the application");
        instructionsContent.AppendLine("- Access the Aspire Dashboard to monitor services");
        instructionsContent.AppendLine("- Services are configured with telemetry, service discovery, and resilience patterns");

        await File.WriteAllTextAsync(copilotInstructionsPath, instructionsContent.ToString(), cancellationToken);
        logger.LogInformation("Created Copilot instructions file at: {Path}", copilotInstructionsPath);
    }

    private async Task ConfigureMcpSettingsAsync(DirectoryInfo workspacePath, CancellationToken cancellationToken)
    {
        var mcpSettingsPath = Path.Combine(workspacePath.FullName, McpSettingsFileName);
        var mcpSettingsFile = new FileInfo(mcpSettingsPath);

        if (!mcpSettingsFile.Exists)
        {
            logger.LogDebug("MCP settings file does not exist at: {Path}, skipping MCP configuration", mcpSettingsPath);
            return;
        }

        logger.LogDebug("Found MCP settings file at: {Path}", mcpSettingsPath);

        try
        {
            // Read the existing MCP settings
            var mcpSettingsJson = await File.ReadAllTextAsync(mcpSettingsPath, cancellationToken);
            using var jsonDocument = JsonDocument.Parse(mcpSettingsJson);
            var root = jsonDocument.RootElement;

            // Check if Aspire MCP server is already configured
            if (root.TryGetProperty("mcpServers", out var mcpServers))
            {
                if (mcpServers.TryGetProperty("aspire", out _))
                {
                    logger.LogDebug("Aspire MCP server is already configured in MCP settings");
                    return;
                }
            }

            // Add Aspire MCP server configuration
            // Note: This is a simplified version - actual MCP configuration may vary
            // based on the MCP specification and Aspire MCP server implementation
            logger.LogInformation("Aspire MCP server configuration can be added manually to: {Path}", mcpSettingsPath);
            logger.LogInformation("Add the Aspire MCP server to enable enhanced coding agent capabilities");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse MCP settings file, it may be malformed");
        }
    }
}
