// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Cli.Mcp.Skills;

/// <summary>
/// Discovers and provides skills from vendor-specific directories.
/// Scans directories like ~/.claude/skills/, ~/.cursor/skills/, ~/.copilot/skills/, etc.
/// </summary>
internal static partial class VendorSkillsSource
{
    private const string DefaultSkillFileName = "SKILL.md";

    private static readonly string[] s_vendorSkillPaths = GetVendorSkillPaths();

    private static string[] GetVendorSkillPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var paths = new List<string>();

        if (!string.IsNullOrEmpty(home))
        {
            // Standard vendor paths following FastMCP conventions
            paths.Add(Path.Combine(home, ".claude", "skills"));
            paths.Add(Path.Combine(home, ".cursor", "skills"));
            paths.Add(Path.Combine(home, ".copilot", "skills"));
            paths.Add(Path.Combine(home, ".gemini", "skills"));
            paths.Add(Path.Combine(home, ".codex", "skills"));
            paths.Add(Path.Combine(home, ".config", "agents", "skills"));      // Goose
            paths.Add(Path.Combine(home, ".config", "opencode", "skills"));    // OpenCode

            // Aspire-specific skills directory
            paths.Add(Path.Combine(home, ".aspire", "skills"));
        }

        // System-level codex skills (Unix-style)
        if (!OperatingSystem.IsWindows())
        {
            paths.Add("/etc/codex/skills");
        }

        return [.. paths];
    }

    /// <summary>
    /// Lists all vendor skills discovered from known skill directories.
    /// </summary>
    public static ValueTask<IReadOnlyList<SkillInfo>> ListSkillsAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future async I/O

        var skills = new List<SkillInfo>();
        var seenSkillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var vendorPath in s_vendorSkillPaths)
        {
            if (!Directory.Exists(vendorPath))
            {
                continue;
            }

            foreach (var skillDir in Directory.EnumerateDirectories(vendorPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var skillFile = Path.Combine(skillDir, DefaultSkillFileName);
                if (!File.Exists(skillFile))
                {
                    continue;
                }

                var skillName = Path.GetFileName(skillDir);

                // Skip if we've already seen this skill name (first path wins)
                if (!seenSkillNames.Add(skillName))
                {
                    continue;
                }

                var description = ExtractDescription(skillFile);
                skills.Add(new SkillInfo
                {
                    Name = skillName,
                    Description = description ?? $"Skill from {Path.GetFileName(Path.GetDirectoryName(vendorPath) ?? vendorPath)}"
                });
            }
        }

        return new ValueTask<IReadOnlyList<SkillInfo>>(skills);
    }

    /// <summary>
    /// Gets a vendor skill by name.
    /// </summary>
    public static ValueTask<SkillContent?> GetSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future async I/O

        foreach (var vendorPath in s_vendorSkillPaths)
        {
            if (!Directory.Exists(vendorPath))
            {
                continue;
            }

            var skillDir = Path.Combine(vendorPath, skillName);
            var skillFile = Path.Combine(skillDir, DefaultSkillFileName);

            if (File.Exists(skillFile))
            {
                var content = File.ReadAllText(skillFile);

                return new ValueTask<SkillContent?>(new SkillContent
                {
                    Name = skillName,
                    Content = content.Trim()
                });
            }
        }

        return new ValueTask<SkillContent?>((SkillContent?)null);
    }

    /// <summary>
    /// Extracts description from SKILL.md frontmatter or first line.
    /// </summary>
    private static string? ExtractDescription(string skillFile)
    {
        try
        {
            // Read just the first few lines to extract description
            using var reader = new StreamReader(skillFile);
            var firstLine = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return null;
            }

            // Check for YAML frontmatter
            if (firstLine.Trim() == "---")
            {
                // Parse frontmatter for description
                while (reader.ReadLine() is { } line)
                {
                    if (line.Trim() == "---")
                    {
                        break;
                    }

                    var match = DescriptionPattern().Match(line);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim().Trim('"', '\'');
                    }
                }

                // After frontmatter, look for first meaningful content
                return FindFirstMeaningfulLine(reader);
            }

            // No frontmatter, use first meaningful line
            if (firstLine.StartsWith('#'))
            {
                // Skip heading, get next meaningful line
                return FindFirstMeaningfulLine(reader);
            }

            return firstLine.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFirstMeaningfulLine(StreamReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith('#'))
            {
                // Truncate if too long
                return trimmed.Length > 200 ? trimmed[..197] + "..." : trimmed;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^description:\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DescriptionPattern();
}
