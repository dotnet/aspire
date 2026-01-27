// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Lightweight metadata about a skill for discovery.
/// </summary>
internal sealed class SkillInfo
{
    /// <summary>
    /// Gets the unique identifier for the skill.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the human-readable description of the skill.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the MCP resource URI for the skill.
    /// </summary>
    public string Uri => $"skill://{Name}";
}
