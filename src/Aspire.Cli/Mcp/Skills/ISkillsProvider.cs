// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Provides unified access to skills from multiple sources.
/// Skills are exposed as MCP resources with the <c>skill://</c> URI scheme.
/// </summary>
internal interface ISkillsProvider
{
    /// <summary>
    /// Lists all available skills from all registered sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of skill metadata for discovery.</returns>
    ValueTask<IReadOnlyList<SkillInfo>> ListSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a skill by its identifier.
    /// </summary>
    /// <param name="skillName">The skill name (e.g., "aspire-pair-programmer").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The skill content, or null if not found.</returns>
    ValueTask<SkillContent?> GetSkillAsync(string skillName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a skill to disk in the specified directory.
    /// </summary>
    /// <param name="skillName">The skill name (used as the directory name).</param>
    /// <param name="content">The skill content (markdown).</param>
    /// <param name="description">Optional description for the skill frontmatter.</param>
    /// <param name="targetDirectory">The target directory (e.g., ~/.aspire/skills/). If null, prompts the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full path to the saved skill file.</returns>
    ValueTask<string> SaveSkillAsync(
        string skillName,
        string content,
        string? description = null,
        string? targetDirectory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default skills directory for persisting user skills.
    /// </summary>
    /// <returns>The default path (e.g., ~/.aspire/skills/).</returns>
    string GetDefaultSkillsDirectory();
}
