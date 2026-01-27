// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Provides unified access to skills from multiple sources.
/// Skills are exposed as MCP resources with the <c>skill://</c> URI scheme.
/// Sources include:
/// <list type="bullet">
/// <item>Built-in Aspire skills with MCP-specific knowledge</item>
/// <item>Vendor skills from platform directories (~/.claude/skills/, ~/.cursor/skills/, etc.)</item>
/// </list>
/// </summary>
internal sealed class SkillsProvider(ILogger<SkillsProvider> logger, IInteractionService interactionService) : ISkillsProvider
{
    private readonly ILogger<SkillsProvider> _logger = logger;
    private readonly IInteractionService _interactionService = interactionService;
    private const string DefaultSkillFileName = "SKILL.md";

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<SkillInfo>> ListSkillsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing all available skills");

        // Get built-in skills first (they take precedence)
        var builtInSkills = await BuiltInSkillsSource.ListSkillsAsync(cancellationToken).ConfigureAwait(false);
        var builtInNames = new HashSet<string>(builtInSkills.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);

        // Get vendor skills (excluding any that conflict with built-in names)
        var vendorSkills = await VendorSkillsSource.ListSkillsAsync(cancellationToken).ConfigureAwait(false);
        var uniqueVendorSkills = vendorSkills.Where(s => !builtInNames.Contains(s.Name));

        // Combine with built-in skills first
        var allSkills = builtInSkills.Concat(uniqueVendorSkills).ToList();

        _logger.LogDebug("Found {Count} skills ({BuiltIn} built-in, {Vendor} vendor)",
            allSkills.Count, builtInSkills.Count, allSkills.Count - builtInSkills.Count);

        return allSkills;
    }

    /// <inheritdoc />
    public async ValueTask<SkillContent?> GetSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting skill: {SkillName}", skillName);

        // Try built-in skills first (they take precedence)
        var skill = await BuiltInSkillsSource.GetSkillAsync(skillName, cancellationToken).ConfigureAwait(false);

        if (skill is not null)
        {
            _logger.LogDebug("Found built-in skill: {SkillName}", skillName);
            return skill;
        }

        // Try vendor skills
        skill = await VendorSkillsSource.GetSkillAsync(skillName, cancellationToken).ConfigureAwait(false);

        if (skill is not null)
        {
            _logger.LogDebug("Found vendor skill: {SkillName}", skillName);
            return skill;
        }

        _logger.LogDebug("Skill not found: {SkillName}", skillName);
        return null;
    }

    /// <inheritdoc />
    public async ValueTask<string> SaveSkillAsync(
        string skillName,
        string content,
        string? description = null,
        string? targetDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        _logger.LogDebug("Saving skill: {SkillName}", skillName);

        // If no target directory specified, prompt the user
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            var defaultDir = GetDefaultSkillsDirectory();
            targetDirectory = await _interactionService.PromptForStringAsync(
                "Enter the directory to save the skill",
                defaultValue: defaultDir,
                required: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Expand environment variables and ~ for home directory
        targetDirectory = ExpandPath(targetDirectory);

        // Create the skill directory (skillName becomes the folder name)
        var skillDir = Path.Combine(targetDirectory, skillName);
        Directory.CreateDirectory(skillDir);

        // Build the skill file content with frontmatter
        var fileContent = BuildSkillFileContent(skillName, content, description);

        // Write the skill file
        var skillFilePath = Path.Combine(skillDir, DefaultSkillFileName);
        await File.WriteAllTextAsync(skillFilePath, fileContent, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Saved skill '{SkillName}' to {Path}", skillName, skillFilePath);
        _interactionService.DisplaySuccess($"Skill '{skillName}' saved to {skillFilePath}");

        return skillFilePath;
    }

    /// <inheritdoc />
    public string GetDefaultSkillsDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".aspire", "skills");
    }

    private static string BuildSkillFileContent(string skillName, string content, string? description)
    {
        // Always include name in frontmatter, description is optional
        var descriptionLine = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : $"\ndescription: {description}";

        // Add YAML frontmatter with name and optional description
        return $"""
            ---
            name: {skillName}{descriptionLine}
            ---

            {content.Trim()}
            """;
    }

    private static string ExpandPath(string path)
    {
        // Expand ~ to home directory
        if (path.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        // Expand environment variables
        return Environment.ExpandEnvironmentVariables(path);
    }

    /// <summary>
    /// Parses a skill name from a skill:// URI.
    /// </summary>
    /// <param name="uri">The skill URI (e.g., "skill://aspire-pair-programmer").</param>
    /// <returns>The skill name, or null if the URI is invalid.</returns>
    public static string? ParseSkillName(string? uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return null;
        }

        const string prefix = "skill://";
        if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var skillName = uri[prefix.Length..];

        // Remove any query string or fragment
        var queryIndex = skillName.IndexOf('?');
        if (queryIndex >= 0)
        {
            skillName = skillName[..queryIndex];
        }

        var fragmentIndex = skillName.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            skillName = skillName[..fragmentIndex];
        }

        return string.IsNullOrEmpty(skillName) ? null : skillName;
    }
}
