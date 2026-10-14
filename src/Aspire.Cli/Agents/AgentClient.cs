// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Agents.Copilot;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Agents;

/// <summary>
/// A supported client group and its configuration and copied-skill destinations.
/// </summary>
internal sealed class AgentClient
{
    private static readonly string s_universalSkillsDirectory = Path.Combine(".agents", "skills");

    public static readonly AgentClient Copilot = new()
    {
        Id = "copilot",
        DisplayName = AgentCommandStrings.Environment_Copilot,
        Environments = AgentEnvironment.CopilotCli | AgentEnvironment.CopilotApp | AgentEnvironment.VsCode,
        ProjectPluginDirectory = Path.Combine(".github", "copilot"),
        GlobalPluginDirectory = static (context, environment) =>
            AgentPath.Expand(CopilotPaths.GetConfigDirectory(context.HomeDirectory, environment), context),
        ManagedPluginDirectory = static (context, environment) =>
            GetManagedPluginDirectory(context, environment, "GitHubCopilot", "github-copilot"),
        // Copilot also consumes Claude project settings, but not Claude user settings.
        // https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-config-dir-reference
        ProjectPluginSettingsFiles =
        [
            Path.Combine(".claude", "settings.json"),
            Path.Combine(".claude", "settings.local.json"),
            Path.Combine(".github", "copilot", "settings.json"),
            Path.Combine(".github", "copilot", "settings.local.json")
        ],
        GlobalMcpConfigPath = static (context, environment) =>
            Path.Combine(CopilotPaths.GetConfigDirectory(context.HomeDirectory, environment), "mcp-config.json"),
        McpServersPropertyName = "mcpServers",
        McpJsonDocumentOptions = ConfigurationHelper.ParseOptions,
        GlobalMcpServerConfiguration = new JsonObject
        {
            ["type"] = "local",
            ["command"] = "aspire",
            ["args"] = new JsonArray("agent", "mcp"),
            ["env"] = new JsonObject
            {
                ["DOTNET_ROOT"] = "${DOTNET_ROOT}"
            },
            ["tools"] = new JsonArray("*")
        },
        ProjectMcpRepairFiles = [Path.Combine(".vscode", "mcp.json"), Path.Combine(".github", "copilot", "mcp.json")]
    };

    public static readonly AgentClient ClaudeCode = new()
    {
        Id = "claude",
        DisplayName = AgentCommandStrings.Environment_ClaudeCode,
        Environments = AgentEnvironment.ClaudeCode,
        ProjectPluginDirectory = ClaudeCodePaths.ConfigDirectoryName,
        GlobalPluginDirectory = ClaudeCodePaths.GetConfigDirectory,
        ManagedPluginDirectory = static (context, environment) =>
            GetManagedPluginDirectory(context, environment, "ClaudeCode", "claude-code"),
        ProjectPluginSettingsFiles =
        [
            Path.Combine(".claude", "settings.json"),
            Path.Combine(".claude", "settings.local.json")
        ],
        UsesManagedSettingsFragments = true,
        ProjectSkillsDirectory = Path.Combine(ClaudeCodePaths.ConfigDirectoryName, "skills"),
        GlobalSkillsDirectory = static (context, environment) =>
            Path.Combine(ClaudeCodePaths.GetConfigDirectory(context, environment), "skills"),
        GlobalMcpConfigPath = ClaudeCodePaths.GetGlobalMcpConfigPath,
        McpServersPropertyName = "mcpServers"
    };

    public static readonly AgentClient OpenCode = new()
    {
        Id = "opencode",
        DisplayName = AgentCommandStrings.Environment_OpenCode,
        Environments = AgentEnvironment.OpenCode,
        ProjectPluginDirectory = string.Empty,
        GlobalPluginDirectory = static (context, environment) =>
            Path.Combine(AgentPath.GetOverride("XDG_CONFIG_HOME", context, environment) ??
                Path.Combine(context.HomeDirectory.FullName, ".config"), "opencode"),
        // https://opencode.ai/docs/config/#managed-settings
        ManagedPluginDirectory = static (context, environment) => environment.IsWindows()
            ? Path.Combine(AgentPath.GetOverride("ProgramData", context, environment) ??
                Path.Combine(Path.GetPathRoot(context.HomeDirectory.FullName)!, "ProgramData"), "opencode")
            : environment.IsMacOS()
                ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "Library", "Application Support", "opencode")
                : Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", "opencode"),
        UsesSkillsCatalog = true,
        ProjectMcpConfigPath = OpenCodePaths.GetMcpConfigPath,
        GlobalMcpConfigPath = OpenCodePaths.GetGlobalMcpConfigPath,
        McpServersPropertyName = "mcp",
        McpJsonDocumentOptions = ConfigurationHelper.ParseOptions,
        McpSchema = "https://opencode.ai/config.json",
        McpServerConfiguration = new JsonObject
        {
            ["type"] = "local",
            ["command"] = new JsonArray("aspire", "agent", "mcp"),
            ["enabled"] = true
        }
    };

    public static IReadOnlyList<AgentClient> All { get; } = [Copilot, ClaudeCode, OpenCode];

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required AgentEnvironment Environments { get; init; }
    public required string ProjectPluginDirectory { get; init; }
    public required Func<CliExecutionContext, IEnvironment, string> GlobalPluginDirectory { get; init; }
    public required Func<CliExecutionContext, IEnvironment, string> ManagedPluginDirectory { get; init; }
    public IReadOnlyList<string> ProjectPluginSettingsFiles { get; init; } = [];
    public bool UsesSkillsCatalog { get; init; }
    public bool UsesManagedSettingsFragments { get; init; }
    public string ProjectSkillsDirectory { get; init; } = s_universalSkillsDirectory;
    public Func<CliExecutionContext, IEnvironment, string> GlobalSkillsDirectory { get; init; }
        = static (context, _) => Path.Combine(context.HomeDirectory.FullName, s_universalSkillsDirectory);
    public Func<DirectoryInfo, string> ProjectMcpConfigPath { get; init; }
        = static directory => Path.Combine(directory.FullName, ".mcp.json");
    public required Func<CliExecutionContext, IEnvironment, string> GlobalMcpConfigPath { get; init; }
    public required string McpServersPropertyName { get; init; }
    public JsonDocumentOptions McpJsonDocumentOptions { get; init; }
    public string? McpSchema { get; init; }
    public JsonObject McpServerConfiguration { get; init; } = new()
    {
        ["command"] = "aspire",
        ["args"] = new JsonArray("agent", "mcp")
    };
    public JsonObject? GlobalMcpServerConfiguration { get; init; }
    public IReadOnlyList<string> ProjectMcpRepairFiles { get; init; } = [];

    public bool Includes(AgentEnvironment environments) => (Environments & environments) != AgentEnvironment.None;

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        return scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ProjectSkillsDirectory)
            : GlobalSkillsDirectory(executionContext, environment);
    }

    public string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        return scope is AgentConfigurationScope.Project
            ? ProjectMcpConfigPath(workspaceRoot)
            : GlobalMcpConfigPath(executionContext, environment);
    }

    public IEnumerable<(string Path, string ServersPropertyName, bool ConfigureServer)> GetMcpConfigPaths(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        yield return (GetMcpConfigPath(scope, workspaceRoot, executionContext, environment), McpServersPropertyName, true);
        if (scope is AgentConfigurationScope.Project)
        {
            // Keep existing editor/CLI repairs, but never let discovery expand the
            // selected scope or authorize writes for an unselected client.
            foreach (var path in ProjectMcpRepairFiles)
            {
                yield return (Path.Combine(workspaceRoot.FullName, path), "servers", false);
            }
        }
    }

    public override string ToString() => Id;

    private static string GetManagedPluginDirectory(CliExecutionContext context, IEnvironment environment, string productName, string unixName)
    {
        return environment.IsWindows()
            ? Path.Combine(AgentPath.GetOverride("ProgramFiles", context, environment) ??
                Path.Combine(Path.GetPathRoot(context.HomeDirectory.FullName)!, "Program Files"), productName)
            : environment.IsMacOS()
                ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "Library", "Application Support", productName)
                : Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", unixName);
    }
}

internal enum AgentConfigurationScope
{
    Project,
    Global
}
