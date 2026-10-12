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
    public static readonly AgentClient Copilot = new("copilot");
    public static readonly AgentClient ClaudeCode = new("claude");
    public static readonly AgentClient OpenCode = new("opencode");

    public static IReadOnlyList<AgentClient> All { get; } = [Copilot, ClaudeCode, OpenCode];

    private AgentClient(string id) => Id = id;

    public string Id { get; }

    public string DisplayName => Id switch
    {
        "copilot" => AgentCommandStrings.Environment_Copilot,
        "claude" => "Claude Code",
        "opencode" => "OpenCode",
        _ => throw new InvalidOperationException()
    };

    public string Description => Id switch
    {
        "copilot" => AgentCommandStrings.Agent_CopilotPlatforms,
        "claude" => AgentCommandStrings.Agent_ClaudePlatforms,
        "opencode" => AgentCommandStrings.Agent_OpenCodePlatforms,
        _ => throw new InvalidOperationException()
    };

    public bool Includes(AgentClientKind client) => Id switch
    {
        "copilot" => client is AgentClientKind.CopilotCli or AgentClientKind.CopilotApp or AgentClientKind.VsCode,
        "claude" => client is AgentClientKind.ClaudeCode,
        "opencode" => client is AgentClientKind.OpenCode,
        _ => false
    };

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        if (this == ClaudeCode)
        {
            return scope is AgentConfigurationScope.Project
                ? Path.Combine(workspaceRoot.FullName, ".claude", "skills")
                : Path.Combine(GetClaudeConfigDirectory(executionContext, environment), "skills");
        }

        // Copilot (including VS Code) and OpenCode discover this shared compatibility
        // location. Their configuration overrides do not relocate it.
        // https://docs.github.com/en/copilot/concepts/agents/about-agent-skills
        // https://opencode.ai/docs/skills/
        var root = scope is AgentConfigurationScope.Project ? workspaceRoot : executionContext.HomeDirectory;
        return Path.Combine(root.FullName, ".agents", "skills");
    }

    public string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment)
    {
        if (scope is AgentConfigurationScope.Project)
        {
            return Path.Combine(workspaceRoot.FullName, this == OpenCode ? "opencode.jsonc" : ".mcp.json");
        }

        if (this == Copilot)
        {
            return Path.Combine(CopilotPaths.GetConfigDirectory(executionContext.HomeDirectory, environment), "mcp-config.json");
        }

        if (this == ClaudeCode)
        {
            // CLAUDE_CONFIG_DIR moves the state file into that directory, unlike ~/.claude.json.
            // https://code.claude.com/docs/en/env-vars
            return GetOverride("CLAUDE_CONFIG_DIR", executionContext, environment) is { } directory
                ? Path.Combine(directory, ".claude.json")
                : Path.Combine(executionContext.HomeDirectory.FullName, ".claude.json");
        }

        if (GetOverride("OPENCODE_CONFIG", executionContext, environment) is { } configFile)
        {
            return configFile;
        }

        var configDirectory = GetOverride("OPENCODE_CONFIG_DIR", executionContext, environment)
            ?? Path.Combine(GetOverride("XDG_CONFIG_HOME", executionContext, environment)
                ?? Path.Combine(executionContext.HomeDirectory.FullName, ".config"), "opencode");
        return Path.Combine(configDirectory, "opencode.jsonc");
    }

    public AgentEnvironmentApplicator? CreateMcpApplicator(string configPath, AgentConfigurationScope scope)
    {
        var isOpenCode = this == OpenCode;
        if (McpConfigFileHelper.HasServerConfigured(configPath, isOpenCode ? "mcp" : "mcpServers", "aspire",
            isOpenCode ? OpenCodeAgentEnvironmentScanner.RemoveJsonComments : null))
        {
            return null;
        }

        return new AgentEnvironmentApplicator(
            string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_McpConfigurationDescription, DisplayName, configPath),
            cancellationToken => isOpenCode
                ? OpenCodeAgentEnvironmentScanner.ApplyMcpConfigurationAsync(configPath, cancellationToken)
                : this == Copilot && scope is AgentConfigurationScope.User
                    ? CopilotAgentEnvironmentScanner.ApplyMcpConfigurationAsync(Path.GetDirectoryName(configPath)!, cancellationToken)
                    : ClaudeCodeAgentEnvironmentScanner.ApplyAspireMcpConfigurationAsync(configPath, cancellationToken))
        {
            TargetPath = configPath
        };
    }

    internal static string GetClaudeConfigDirectory(CliExecutionContext executionContext, IEnvironment environment)
        => GetOverride("CLAUDE_CONFIG_DIR", executionContext, environment) ?? Path.Combine(executionContext.HomeDirectory.FullName, ".claude");

    private static string? GetOverride(string name, CliExecutionContext executionContext, IEnvironment environment)
        => environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? Path.GetFullPath(value, executionContext.WorkingDirectory.FullName)
            : null;

    public override string ToString() => Id;
}

internal enum AgentConfigurationScope
{
    Project,
    User
}
