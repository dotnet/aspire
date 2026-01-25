// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileSystemGlobbing;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Filters out files that should be ignored based on glob patterns.
/// </summary>
public sealed class IgnorePathFilter
{
    private readonly List<string> _ignorePatterns;
    private readonly Matcher _matcher;

    public IgnorePathFilter(IEnumerable<string> ignorePatterns)
    {
        _ignorePatterns = ignorePatterns.ToList();
        _matcher = new Matcher();

        foreach (var pattern in _ignorePatterns)
        {
            _matcher.AddInclude(pattern);
        }
    }

    /// <summary>
    /// Checks if a file should be ignored.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file should be ignored.</returns>
    public bool ShouldIgnore(string filePath)
    {
        // Normalize path separators
        var normalizedPath = filePath.Replace('\\', '/');

        // Check against all patterns using the Matcher
        var result = _matcher.Match(normalizedPath);
        return result.HasMatches;
    }

    /// <summary>
    /// Splits files into ignored and active lists.
    /// </summary>
    /// <param name="files">The files to split.</param>
    /// <returns>A tuple of (ignoredFiles, activeFiles).</returns>
    public (List<string> Ignored, List<string> Active) SplitFiles(IEnumerable<string> files)
    {
        var ignored = new List<string>();
        var active = new List<string>();

        foreach (var file in files)
        {
            if (ShouldIgnore(file))
            {
                ignored.Add(file);
            }
            else
            {
                active.Add(file);
            }
        }

        return (ignored, active);
    }

    /// <summary>
    /// Gets the patterns being used for filtering.
    /// </summary>
    public IReadOnlyList<string> Patterns => _ignorePatterns;
}
