// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents.ClaudeCode;

internal sealed class ClaudeCodeAgentClient(CliExecutionContext context, IEnvironment environment)
    : AgentClient(ClaudeCodeId, AgentCommandStrings.Environment_ClaudeCode, AgentEnvironment.ClaudeCode)
{
    private static readonly McpConfigFileHelper.Configuration s_mcp = new() { ServersPropertyName = "mcpServers" };

    public override string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => Path.Combine(scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ClaudeCodePaths.ConfigDirectoryName)
            : ClaudeCodePaths.GetConfigDirectory(context, environment), "skills");

    public override IEnumerable<string> GetLocalSkillDirectories(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
    {
        yield return GetSkillBaseDirectory(scope, workspaceRoot);
    }

    public override Task<bool> HasNativeSkillsRegistrationAsync(DirectoryInfo workspaceRoot, CancellationToken cancellationToken)
        => AspireSkillsConfiguration.HasPluginRegistrationAsync(GetSettingsFiles(workspaceRoot), cancellationToken);

    public override AgentFileEdit CreateNativeSkillsEdit(DirectoryInfo workspaceRoot, AgentConfigurationScope scope)
    {
        var directory = scope is AgentConfigurationScope.Project
            ? Path.Combine(workspaceRoot.FullName, ClaudeCodePaths.ConfigDirectoryName)
            : ClaudeCodePaths.GetConfigDirectory(context, environment);

        return AspireSkillsConfiguration.CreatePluginEdit(
            this, Path.Combine(directory, "settings.json"), GetSettingsFiles(workspaceRoot));
    }

    public override string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => scope is AgentConfigurationScope.Project ? Path.Combine(workspaceRoot.FullName, ".mcp.json")
            : ClaudeCodePaths.GetGlobalMcpConfigPath(context, environment);

    public override Task<IReadOnlyList<AgentFileEdit>> CreateMcpEditsAsync(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AgentFileEdit>>(
            [McpConfigFileHelper.CreateEdit(this, GetMcpConfigPath(scope, workspaceRoot), s_mcp, true, scope)]);
    }

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
