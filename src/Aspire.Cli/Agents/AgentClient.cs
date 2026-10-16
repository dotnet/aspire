// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// A supported client's identity and configuration behavior.
/// </summary>
internal abstract class AgentClient(
    string id,
    string displayName,
    AgentEnvironment environments)
{
    public const string CopilotId = "copilot";
    public const string ClaudeCodeId = "claude";
    public const string OpenCodeId = "opencode";
    public static IReadOnlyList<string> Ids { get; } = [CopilotId, ClaudeCodeId, OpenCodeId];

    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public AgentEnvironment Environments { get; } = environments;

    public bool Includes(AgentEnvironment environments) => (Environments & environments) != AgentEnvironment.None;

    public abstract string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot);

    // Include compatibility locations the client loads, not just the CLI's install destination.
    public abstract IEnumerable<string> GetLocalSkillDirectories(AgentConfigurationScope scope, DirectoryInfo workspaceRoot);

    public abstract Task<bool> HasNativeSkillsRegistrationAsync(DirectoryInfo workspaceRoot, CancellationToken cancellationToken);

    public abstract AgentFileEdit CreateNativeSkillsEdit(DirectoryInfo workspaceRoot, AgentConfigurationScope scope);

    public abstract string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot);

    public abstract Task<IReadOnlyList<AgentFileEdit>> CreateMcpEditsAsync(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope, CancellationToken cancellationToken);

    public override string ToString() => Id;
}

internal enum AgentConfigurationScope
{
    Project,
    Global
}
