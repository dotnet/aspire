// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    public CriticalFileDetector(IEnumerable<string> triggerAllPatterns, IEnumerable<string> excludePatterns)
    {
        _triggerAllPatterns = triggerAllPatterns.ToList();
        _excludePatterns = excludePatterns.ToList();

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
}
