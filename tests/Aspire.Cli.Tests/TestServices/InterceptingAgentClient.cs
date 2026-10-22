// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Agents;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class InterceptingAgentClient(IAgentClient inner, Func<Task> afterUpdate) : IAgentClient
{
    public string Id => inner.Id;
    public string DisplayName => inner.DisplayName;
    public AgentEnvironment Environments => inner.Environments;

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => inner.GetSkillBaseDirectory(scope, workspaceRoot);

    public IEnumerable<string> GetLocalSkillDirectories(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => inner.GetLocalSkillDirectories(scope, workspaceRoot);

    public Task<bool> HasIntegrationRegistrationAsync(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope, CancellationToken cancellationToken)
        => inner.HasIntegrationRegistrationAsync(workspaceRoot, scope, cancellationToken);

    public string GetIntegrationConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => inner.GetIntegrationConfigPath(scope, workspaceRoot);

    public async Task UpdateIntegrationConfigAsync(
        JsonObject config, DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        AgentConfiguration edit, CancellationToken cancellationToken)
    {
        await inner.UpdateIntegrationConfigAsync(config, workspaceRoot, scope, edit, cancellationToken);
        await afterUpdate();
    }

    public string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => inner.GetMcpConfigPath(scope, workspaceRoot);

    public IEnumerable<(string Path, bool ConfigureServer)> GetMcpConfigPaths(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => inner.GetMcpConfigPaths(scope, workspaceRoot);

    public Task<(JsonObject Config, string Path)> ReadMcpConfigAsync(
        string configFilePath, DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        AgentConfiguration edit, CancellationToken cancellationToken)
        => inner.ReadMcpConfigAsync(configFilePath, workspaceRoot, scope, edit, cancellationToken);

    public (bool Repaired, bool Added) UpdateMcpConfig(
        JsonObject config, string configFilePath, bool configureServer, AgentConfigurationScope scope)
        => inner.UpdateMcpConfig(config, configFilePath, configureServer, scope);
}
