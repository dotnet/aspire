// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Full content of a skill for reading.
/// </summary>
internal sealed class SkillContent
{
    /// <summary>
    /// Gets the unique identifier for the skill.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the skill content (typically markdown).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the MIME type of the content.
    /// </summary>
    public string MimeType { get; init; } = "text/markdown";
}
