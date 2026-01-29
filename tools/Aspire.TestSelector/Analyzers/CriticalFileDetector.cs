// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Detects critical files that should trigger all tests.
/// </summary>
public sealed class CriticalFileDetector
{
    private readonly List<string> _triggerAllPatterns;
    private readonly List<string> _excludePatterns;
    private readonly Matcher _triggerMatcher;
    private readonly Matcher _excludeMatcher;
    private readonly Dictionary<string, string> _patternToCategory;

    public CriticalFileDetector(IEnumerable<string> triggerAllPatterns, IEnumerable<string> excludePatterns)
        : this(triggerAllPatterns, excludePatterns, [])
    {
    }

    public CriticalFileDetector(IEnumerable<string> triggerAllPatterns, IEnumerable<string> excludePatterns, Dictionary<string, string> patternToCategory)
    {
        _triggerAllPatterns = triggerAllPatterns.ToList();
        _excludePatterns = excludePatterns.ToList();
        _patternToCategory = patternToCategory;

        _triggerMatcher = new Matcher();
        foreach (var pattern in _triggerAllPatterns)
        {
            _triggerMatcher.AddInclude(pattern);
        }

        _excludeMatcher = new Matcher();
        foreach (var pattern in _excludePatterns)
        {
            _excludeMatcher.AddInclude(pattern);
        }
    }

    /// <summary>
    /// Checks if a file is a critical file that should trigger all tests.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="matchedPattern">The pattern that matched (if any).</param>
    /// <returns>True if the file is critical.</returns>
    public bool IsCriticalFile(string filePath, out string? matchedPattern)
    {
        matchedPattern = null;
        var normalizedPath = filePath.Replace('\\', '/');

        // Check if it matches any exclude pattern first
        var excludeResult = _excludeMatcher.Match(normalizedPath);
        if (excludeResult.HasMatches)
        {
            return false;
        }

        // Check if it matches any trigger pattern
        var triggerResult = _triggerMatcher.Match(normalizedPath);
        if (triggerResult.HasMatches)
        {
            // Find which pattern matched for reporting
            matchedPattern = FindMatchingPattern(normalizedPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks a list of files and returns the first critical file found.
    /// </summary>
    /// <param name="files">The files to check.</param>
    /// <returns>A tuple of (criticalFile, matchedPattern) or (null, null) if none found.</returns>
    public (string? File, string? Pattern) FindFirstCriticalFile(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (IsCriticalFile(file, out var pattern))
            {
                return (file, pattern);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Checks a list of files and returns the first critical file found with detailed info.
    /// </summary>
    /// <param name="files">The files to check.</param>
    /// <returns>Critical file info or null if none found.</returns>
    public CriticalFileInfo? FindFirstCriticalFileWithDetails(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (IsCriticalFile(file, out var pattern))
            {
                var category = pattern != null && _patternToCategory.TryGetValue(pattern, out var cat) ? cat : null;
                return new CriticalFileInfo
                {
                    FilePath = file,
                    MatchedPattern = pattern ?? "unknown",
                    Category = category
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Finds all critical files in a list.
    /// </summary>
    /// <param name="files">The files to check.</param>
    /// <returns>List of critical files with their matching patterns.</returns>
    public List<(string File, string Pattern)> FindAllCriticalFiles(IEnumerable<string> files)
    {
        var result = new List<(string File, string Pattern)>();

        foreach (var file in files)
        {
            if (IsCriticalFile(file, out var pattern))
            {
                result.Add((file, pattern ?? "unknown"));
            }
        }

        return result;
    }

    private string FindMatchingPattern(string filePath)
    {
        foreach (var pattern in _triggerAllPatterns)
        {
            var singleMatcher = new Matcher();
            singleMatcher.AddInclude(pattern);
            if (singleMatcher.Match(filePath).HasMatches)
            {
                return pattern;
            }
        }

        return "unknown";
    }

    /// <summary>
    /// Gets the trigger patterns being used.
    /// </summary>
    public IReadOnlyList<string> TriggerPatterns => _triggerAllPatterns;

    /// <summary>
    /// Gets the exclude patterns being used.
    /// </summary>
    public IReadOnlyList<string> ExcludePatterns => _excludePatterns;

    /// <summary>
    /// Creates a CriticalFileDetector from category configurations.
    /// Extracts triggerAll patterns from categories that have TriggerAll=true.
    /// </summary>
    /// <param name="categories">The category configurations.</param>
    /// <returns>A CriticalFileDetector for the triggerAll patterns.</returns>
    public static CriticalFileDetector FromCategories(Dictionary<string, CategoryConfig> categories)
    {
        var triggerAllPatterns = new List<string>();
        var patternToCategory = new Dictionary<string, string>();

        foreach (var (categoryName, config) in categories)
        {
            if (config.TriggerAll)
            {
                foreach (var pattern in config.TriggerPaths)
                {
                    triggerAllPatterns.Add(pattern);
                    patternToCategory[pattern] = categoryName;
                }
            }
        }

        // Categories with triggerAll don't have exclude patterns in the new model
        return new CriticalFileDetector(triggerAllPatterns, [], patternToCategory);
    }

    /// <summary>
    /// Gets all categories that have triggerAll enabled.
    /// </summary>
    public IEnumerable<string> GetTriggerAllCategories()
    {
        return _patternToCategory.Values.Distinct();
    }

    /// <summary>
    /// Gets the category for a given trigger pattern.
    /// </summary>
    public string? GetCategoryForPattern(string pattern)
    {
        return _patternToCategory.TryGetValue(pattern, out var category) ? category : null;
    }
}

/// <summary>
/// Information about a critical file match.
/// </summary>
public sealed class CriticalFileInfo
{
    /// <summary>
    /// The file path that was identified as critical.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The pattern that matched the file.
    /// </summary>
    public required string MatchedPattern { get; init; }

    /// <summary>
    /// The category that contains the matching pattern (if known).
    /// </summary>
    public string? Category { get; init; }
}
