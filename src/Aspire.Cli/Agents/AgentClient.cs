// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// A supported client group and its shared or agent-specific skill destinations.
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
        GlobalSkillsDirectory = static (context, _) => Path.Combine(context.HomeDirectory.FullName, s_universalSkillsDirectory)
    };

    public static readonly AgentClient ClaudeCode = new()
    {
        DisplayName = AgentCommandStrings.Environment_ClaudeCode,
        Description = AgentCommandStrings.Agent_ClaudePlatforms,
        Environments = AgentEnvironment.ClaudeCode,
        ProjectSkillsDirectory = Path.Combine(".claude", "skills"),
        GlobalSkillsDirectory = static (context, environment) =>
            Path.Combine(ClaudeCodeAgentEnvironmentScanner.GetConfigDirectory(context, environment), "skills")
    };

    public static readonly AgentClient OpenCode = new()
    {
        DisplayName = AgentCommandStrings.Environment_OpenCode,
        Description = AgentCommandStrings.Agent_OpenCodePlatforms,
        Environments = AgentEnvironment.OpenCode,
        UsesUniversalSkills = true,
        ProjectSkillsDirectory = s_universalSkillsDirectory,
        GlobalSkillsDirectory = static (context, _) => Path.Combine(context.HomeDirectory.FullName, s_universalSkillsDirectory)
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

    public bool Includes(AgentEnvironment environments) => (Environments & environments) != AgentEnvironment.None;

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo? workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        return scope is AgentConfigurationScope.Project
            ? Path.Combine(AgentPath.RequireWorkspaceRoot(workspaceRoot).FullName, ProjectSkillsDirectory)
            : GlobalSkillsDirectory(executionContext, environment);
    }

    internal static string GetUniversalSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext)
        => Path.Combine((scope is AgentConfigurationScope.Project ? workspaceRoot : executionContext.HomeDirectory).FullName, s_universalSkillsDirectory);

    public override string ToString() => DisplayName;
}

internal enum AgentConfigurationScope
{
    Project,
    Global
}
