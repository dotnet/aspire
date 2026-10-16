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
        Plugin = new()
        {
            ProjectDirectory = Path.Combine(".github", "copilot"),
            GlobalDirectory = static (context, environment) =>
                AgentPath.Expand(CopilotPaths.GetConfigDirectory(context.HomeDirectory, environment), context),
            ManagedDirectory = static (context, environment) =>
                GetManagedSettingsDirectory(context, environment, "GitHubCopilot", "github-copilot"),
            // Copilot also consumes Claude project settings, but not Claude user settings.
            // https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-config-dir-reference
            ProjectSettingsFiles =
            [
                Path.Combine(".claude", "settings.json"),
                Path.Combine(".claude", "settings.local.json"),
                Path.Combine(".github", "copilot", "settings.json"),
                Path.Combine(".github", "copilot", "settings.local.json")
            ]
        },
        Mcp = new()
        {
            GlobalConfigPath = static (context, environment) =>
                Path.Combine(CopilotPaths.GetConfigDirectory(context.HomeDirectory, environment), "mcp-config.json"),
            ServersPropertyName = "mcpServers",
            JsonDocumentOptions = ConfigurationHelper.ParseOptions,
            GlobalServerConfiguration = new JsonObject
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
            ProjectRepairFiles = [Path.Combine(".vscode", "mcp.json"), Path.Combine(".github", "copilot", "mcp.json")]
        }
    };

    public static readonly AgentClient ClaudeCode = new()
    {
        Id = "claude",
        DisplayName = AgentCommandStrings.Environment_ClaudeCode,
        Environments = AgentEnvironment.ClaudeCode,
        Plugin = new()
        {
            ProjectDirectory = ClaudeCodePaths.ConfigDirectoryName,
            GlobalDirectory = ClaudeCodePaths.GetConfigDirectory,
            ManagedDirectory = static (context, environment) =>
                GetManagedSettingsDirectory(context, environment, "ClaudeCode", "claude-code"),
            ProjectSettingsFiles =
            [
                Path.Combine(".claude", "settings.json"),
                Path.Combine(".claude", "settings.local.json")
            ],
            UsesManagedSettingsFragments = true
        },
        ProjectSkillsDirectory = Path.Combine(ClaudeCodePaths.ConfigDirectoryName, "skills"),
        GlobalSkillsDirectory = static (context, environment) =>
            Path.Combine(ClaudeCodePaths.GetConfigDirectory(context, environment), "skills"),
        Mcp = new()
        {
            GlobalConfigPath = ClaudeCodePaths.GetGlobalMcpConfigPath,
            ServersPropertyName = "mcpServers"
        }
    };

    public static readonly AgentClient OpenCode = new()
    {
        Id = "opencode",
        DisplayName = AgentCommandStrings.Environment_OpenCode,
        Environments = AgentEnvironment.OpenCode,
        Mcp = new()
        {
            ProjectConfigPath = OpenCodePaths.GetMcpConfigPath,
            GlobalConfigPath = OpenCodePaths.GetGlobalMcpConfigPath,
            ServersPropertyName = "mcp",
            JsonDocumentOptions = ConfigurationHelper.ParseOptions,
            Schema = "https://opencode.ai/config.json",
            ServerConfiguration = new JsonObject
            {
                ["type"] = "local",
                ["command"] = new JsonArray("aspire", "agent", "mcp"),
                ["enabled"] = true
            }
        }
    };

    public static IReadOnlyList<AgentClient> All { get; } = [Copilot, ClaudeCode, OpenCode];

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required AgentEnvironment Environments { get; init; }
    public AgentPluginConfiguration? Plugin { get; init; }
    public required AgentMcpConfiguration Mcp { get; init; }
    public string ProjectSkillsDirectory { get; init; } = s_universalSkillsDirectory;
    public Func<CliExecutionContext, IEnvironment, string> GlobalSkillsDirectory { get; init; }
        = static (context, _) => Path.Combine(context.HomeDirectory.FullName, s_universalSkillsDirectory);

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
            ? Mcp.ProjectConfigPath(workspaceRoot)
            : Mcp.GlobalConfigPath(executionContext, environment);
    }

    public IEnumerable<(string Path, string ServersPropertyName, bool ConfigureServer)> GetMcpConfigPaths(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        yield return (GetMcpConfigPath(scope, workspaceRoot, executionContext, environment), Mcp.ServersPropertyName, true);
        if (scope is AgentConfigurationScope.Project)
        {
            // Keep existing editor/CLI repairs, but never let discovery expand the
            // selected scope or authorize writes for an unselected client.
            foreach (var path in Mcp.ProjectRepairFiles)
            {
                yield return (Path.Combine(workspaceRoot.FullName, path), "servers", false);
            }
        }
    }

    public async Task<IReadOnlyList<AgentFileEdit>> CreateMcpEditsAsync(
        AgentConfigurationScope scope, DirectoryInfo workspaceRoot,
        CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        if (this == OpenCode)
        {
            return [await OpenCodeConfiguration.CreateMcpEditAsync(
                workspaceRoot, scope, executionContext, environment, cancellationToken)];
        }

        return GetMcpConfigPaths(scope, workspaceRoot, executionContext, environment)
            .Select(target => McpConfigFileHelper.CreateEdit(
                this, target.Path, target.ServersPropertyName, target.ConfigureServer, scope)).ToArray();
    }

    public override string ToString() => Id;

    private static string GetManagedSettingsDirectory(CliExecutionContext context, IEnvironment environment, string productName, string unixName)
    {
        return environment.IsWindows()
            ? Path.Combine(AgentPath.GetOverride("ProgramFiles", context, environment) ??
                Path.Combine(Path.GetPathRoot(context.HomeDirectory.FullName)!, "Program Files"), productName)
            : environment.IsMacOS()
                ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "Library", "Application Support", productName)
                : Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", unixName);
    }
}

internal sealed record AgentPluginConfiguration
{
    public required string ProjectDirectory { get; init; }
    public required Func<CliExecutionContext, IEnvironment, string> GlobalDirectory { get; init; }
    public required Func<CliExecutionContext, IEnvironment, string> ManagedDirectory { get; init; }
    public IReadOnlyList<string> ProjectSettingsFiles { get; init; } = [];
    public bool UsesManagedSettingsFragments { get; init; }
}

internal sealed record AgentMcpConfiguration
{
    public Func<DirectoryInfo, string> ProjectConfigPath { get; init; }
        = static directory => Path.Combine(directory.FullName, ".mcp.json");
    public required Func<CliExecutionContext, IEnvironment, string> GlobalConfigPath { get; init; }
    public required string ServersPropertyName { get; init; }
    public JsonDocumentOptions JsonDocumentOptions { get; init; }
    public string? Schema { get; init; }
    public JsonObject ServerConfiguration { get; init; } = new()
    {
        ["command"] = "aspire",
        ["args"] = new JsonArray("agent", "mcp")
    };
    public JsonObject? GlobalServerConfiguration { get; init; }
    public IReadOnlyList<string> ProjectRepairFiles { get; init; } = [];
}

internal enum AgentConfigurationScope
{
    Project,
    Global
}
