// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Handles non-.NET file rules that map to specific test categories.
/// </summary>
public sealed class NonDotNetRulesHandler
{
    private readonly List<(Matcher Matcher, string Pattern, string Category)> _rules;

    public NonDotNetRulesHandler(IEnumerable<NonDotNetRule> rules)
    {
        _rules = [];

        foreach (var rule in rules)
        {
            var matcher = new Matcher();
            matcher.AddInclude(rule.Pattern);
            _rules.Add((matcher, rule.Pattern, rule.Category));
        }
    }

    /// <summary>
    /// Gets the categories triggered by a file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>List of categories triggered by this file.</returns>
    public List<string> GetTriggeredCategories(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var categories = new List<string>();

        foreach (var (matcher, _, category) in _rules)
        {
            if (matcher.Match(normalizedPath).HasMatches)
            {
                if (!categories.Contains(category))
                {
                    categories.Add(category);
                }
            }
        }

        return categories;
    }

    /// <summary>
    /// Gets all categories triggered by a list of files.
    /// </summary>
    /// <param name="files">The files to check.</param>
    /// <returns>Dictionary mapping categories to the files that triggered them.</returns>
    public Dictionary<string, List<string>> GetAllTriggeredCategories(IEnumerable<string> files)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var file in files)
        {
            var categories = GetTriggeredCategories(file);
            foreach (var category in categories)
            {
                if (!result.TryGetValue(category, out var fileList))
                {
                    fileList = [];
                    result[category] = fileList;
                }
                fileList.Add(file);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a file matches any non-.NET rule.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file matches any rule.</returns>
    public bool MatchesAnyRule(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var (matcher, _, _) in _rules)
        {
            if (matcher.Match(normalizedPath).HasMatches)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the first matching rule for a file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>A tuple of (pattern, category) or (null, null) if no match.</returns>
    public (string? Pattern, string? Category) GetFirstMatch(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var (matcher, pattern, category) in _rules)
        {
            if (matcher.Match(normalizedPath).HasMatches)
            {
                return (pattern, category);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Gets the number of rules configured.
    /// </summary>
    public int RuleCount => _rules.Count;
}
