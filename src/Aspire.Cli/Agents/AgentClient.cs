// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Agents.Copilot;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// A supported client group and its configuration and copied-skill destinations.
/// </summary>
internal sealed class AgentClient
{
    private static readonly string s_universalSkillsDirectory = Path.Combine(".agents", "skills");

    public static readonly AgentClient Copilot = new()
    {
        DisplayName = AgentCommandStrings.Environment_Copilot,
        Description = AgentCommandStrings.Agent_CopilotPlatforms,
        Environments = AgentEnvironment.CopilotCli | AgentEnvironment.CopilotApp | AgentEnvironment.VsCode,
        UsesUniversalSkills = true,
        ProjectSkillsDirectory = s_universalSkillsDirectory,
        GlobalSkillsDirectory = static (context, _) => Path.Combine(context.HomeDirectory.FullName, s_universalSkillsDirectory),
        ProjectMcpFileName = ".mcp.json",
        GlobalMcpConfigPath = static (context, environment) =>
            Path.Combine(CopilotPaths.GetConfigDirectory(context.HomeDirectory, environment), "mcp-config.json"),
        McpServersPropertyName = "mcpServers",
        ApplyProjectMcpConfigurationAsync = ClaudeCodeAgentEnvironmentScanner.ApplyAspireMcpConfigurationAsync,
        ApplyGlobalMcpConfigurationAsync = static (path, cancellationToken) =>
            CopilotAgentEnvironmentScanner.ApplyMcpConfigurationAsync(Path.GetDirectoryName(path)!, cancellationToken),
        ProjectMcpRepairFiles = [Path.Combine(".vscode", "mcp.json"), Path.Combine(".github", "copilot", "mcp.json")]
    };

    public static readonly AgentClient ClaudeCode = new()
    {
        DisplayName = AgentCommandStrings.Environment_ClaudeCode,
        Description = AgentCommandStrings.Agent_ClaudePlatforms,
        Environments = AgentEnvironment.ClaudeCode,
        ProjectSkillsDirectory = Path.Combine(".claude", "skills"),
        GlobalSkillsDirectory = static (context, environment) =>
            Path.Combine(ClaudeCodeAgentEnvironmentScanner.GetConfigDirectory(context, environment), "skills"),
        ProjectMcpFileName = ".mcp.json",
        GlobalMcpConfigPath = ClaudeCodeAgentEnvironmentScanner.GetGlobalMcpConfigPath,
        McpServersPropertyName = "mcpServers",
        ApplyProjectMcpConfigurationAsync = ClaudeCodeAgentEnvironmentScanner.ApplyAspireMcpConfigurationAsync,
        ApplyGlobalMcpConfigurationAsync = ClaudeCodeAgentEnvironmentScanner.ApplyAspireMcpConfigurationAsync
    };

    public static readonly AgentClient OpenCode = new()
    {
        DisplayName = AgentCommandStrings.Environment_OpenCode,
        Description = AgentCommandStrings.Agent_OpenCodePlatforms,
        Environments = AgentEnvironment.OpenCode,
        UsesUniversalSkills = true,
        ProjectSkillsDirectory = s_universalSkillsDirectory,
        GlobalSkillsDirectory = static (context, _) => Path.Combine(context.HomeDirectory.FullName, s_universalSkillsDirectory),
        ProjectMcpFileName = "opencode.jsonc",
        GlobalMcpConfigPath = OpenCodeAgentEnvironmentScanner.GetGlobalMcpConfigPath,
        McpServersPropertyName = "mcp",
        McpJsonPreprocessor = OpenCodeAgentEnvironmentScanner.RemoveJsonComments,
        ApplyProjectMcpConfigurationAsync = OpenCodeAgentEnvironmentScanner.ApplyMcpConfigurationAsync,
        ApplyGlobalMcpConfigurationAsync = OpenCodeAgentEnvironmentScanner.ApplyMcpConfigurationAsync
    };

    public static IReadOnlyList<AgentClient> All { get; } = [Copilot, ClaudeCode, OpenCode];

    private AgentClient()
    {
    }

    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required AgentEnvironment Environments { get; init; }
    public bool UsesUniversalSkills { get; init; }
    public required string ProjectSkillsDirectory { get; init; }
    public required Func<CliExecutionContext, IEnvironment, string> GlobalSkillsDirectory { get; init; }
    public required string ProjectMcpFileName { get; init; }
    public required Func<CliExecutionContext, IEnvironment, string> GlobalMcpConfigPath { get; init; }
    public required string McpServersPropertyName { get; init; }
    public Func<string, string>? McpJsonPreprocessor { get; init; }
    public required Func<string, CancellationToken, Task> ApplyProjectMcpConfigurationAsync { get; init; }
    public required Func<string, CancellationToken, Task> ApplyGlobalMcpConfigurationAsync { get; init; }
    public IReadOnlyList<string> ProjectMcpRepairFiles { get; init; } = [];

    public bool Includes(AgentEnvironment environments) => (Environments & environments) != AgentEnvironment.None;

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        return scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ProjectSkillsDirectory)
            : GlobalSkillsDirectory(executionContext, environment);
    }

    internal static string GetUniversalSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext)
        => Path.Combine((scope is AgentConfigurationScope.Project ? workspaceRoot : executionContext.HomeDirectory).FullName, s_universalSkillsDirectory);

    public string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        return scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ProjectMcpFileName)
            : GlobalMcpConfigPath(executionContext, environment);
    }

    public AgentEnvironmentApplicator? CreateMcpApplicator(string configPath, AgentConfigurationScope scope)
    {
        if (McpConfigFileHelper.HasServerConfigured(configPath, McpServersPropertyName, "aspire", McpJsonPreprocessor))
        {
            return null;
        }

        var applyConfiguration = scope is AgentConfigurationScope.Project
            ? ApplyProjectMcpConfigurationAsync
            : ApplyGlobalMcpConfigurationAsync;
        return new AgentEnvironmentApplicator(
            string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_McpConfigurationDescription, DisplayName, configPath),
            cancellationToken => applyConfiguration(configPath, cancellationToken))
        {
            TargetPath = configPath
        };
    }

    public IEnumerable<string> GetMcpRepairPaths(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        yield return GetMcpConfigPath(scope, workspaceRoot, executionContext, environment);
        if (scope is AgentConfigurationScope.Project)
        {
            // Keep existing editor/CLI repairs, but never let discovery expand the
            // selected scope or authorize writes for an unselected client.
            foreach (var path in ProjectMcpRepairFiles)
            {
                yield return Path.Combine(workspaceRoot.FullName, path);
            }
        }
    }

    public override string ToString() => DisplayName;
}

internal enum AgentConfigurationScope
{
    Project,
    Global
}
