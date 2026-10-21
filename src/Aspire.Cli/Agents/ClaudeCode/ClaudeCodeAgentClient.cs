// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents.ClaudeCode;

internal sealed class ClaudeCodeAgentClient(CliExecutionContext context, IEnvironment environment)
    : IAgentClient
{
    public const string ClientId = "claude";

    private static readonly McpConfigFileHelper.Configuration s_mcp = new() { ServersPropertyName = "mcpServers" };

    public string Id => ClientId;
    public string DisplayName { get; } = AgentCommandStrings.Environment_ClaudeCode;
    public AgentEnvironment Environments => AgentEnvironment.ClaudeCode;

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => Path.Combine(scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ClaudeCodePaths.ConfigDirectoryName)
            : ClaudeCodePaths.GetConfigDirectory(context, environment), "skills");

    public IEnumerable<string> GetLocalSkillDirectories(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
    {
        yield return GetSkillBaseDirectory(scope, workspaceRoot);
    }

    public Task<bool> HasIntegrationRegistrationAsync(DirectoryInfo workspaceRoot, CancellationToken cancellationToken)
        => AspireSkillsConfiguration.HasPluginRegistrationAsync(GetSettingsFiles(workspaceRoot), cancellationToken);

    public string GetIntegrationConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
    {
        var directory = scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ClaudeCodePaths.ConfigDirectoryName)
            : ClaudeCodePaths.GetConfigDirectory(context, environment);

        return Path.Combine(directory, "settings.json");
    }

    public Task UpdateIntegrationConfigAsync(
        JsonObject config, DirectoryInfo workspaceRoot, AgentConfigurationScope scope, CancellationToken cancellationToken)
        => AspireSkillsConfiguration.ApplyPluginAsync(config, GetSettingsFiles(workspaceRoot),
            GetIntegrationConfigPath(scope, workspaceRoot), cancellationToken);

    public string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => scope is AgentConfigurationScope.Project ? Path.Combine(workspaceRoot.FullName, ".mcp.json")
            : ClaudeCodePaths.GetGlobalMcpConfigPath(context, environment);

    public IEnumerable<(string Path, bool ConfigureServer)> GetMcpConfigPaths(
        AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => [(GetMcpConfigPath(scope, workspaceRoot), true)];

    public async Task<(JsonObject Config, string Path)> ReadMcpConfigAsync(
        string configFilePath, AgentConfigurationScope scope, CancellationToken cancellationToken)
        => (await McpConfigFileHelper.ReadConfigAsync(configFilePath, s_mcp.JsonDocumentOptions, cancellationToken), configFilePath);

    public (bool Repaired, bool Added) UpdateMcpConfig(
        JsonObject config, string configFilePath, bool configureServer, AgentConfigurationScope scope)
    {
        var repaired = McpConfigFileHelper.UpdateDeprecatedCommand(config, s_mcp.ServersPropertyName);
        var added = configureServer && McpConfigFileHelper.ConfigureMcpServer(config, s_mcp, scope, configFilePath);

        return (repaired, added);
    }

    public override string ToString() => Id;

    private IEnumerable<string> GetSettingsFiles(DirectoryInfo workspaceRoot)
    {
        yield return Path.Combine(ClaudeCodePaths.GetConfigDirectory(context, environment), "settings.json");
        yield return Path.Combine(workspaceRoot.FullName, ".claude", "settings.json");
        yield return Path.Combine(workspaceRoot.FullName, ".claude", "settings.local.json");
        var managed = AgentPath.GetManagedSettingsDirectory(context, environment, "ClaudeCode", "claude-code");
        yield return Path.Combine(managed, "managed-settings.json");
        var fragments = Path.Combine(managed, "managed-settings.d");
        if (Directory.Exists(fragments))
        {
            foreach (var path in Directory.EnumerateFiles(fragments, "*.json").Order(StringComparer.Ordinal))
            {
                if (!Path.GetFileName(path).StartsWith('.'))
                {
                    yield return path;
                }
            }
        }
    }
}
