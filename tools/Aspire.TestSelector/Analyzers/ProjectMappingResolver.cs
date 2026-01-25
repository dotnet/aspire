// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.TestSelector.Models;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Resolves test projects from changed files using projectMappings configuration.
/// Supports {name} capture group substitution for flexible source-to-test mapping.
/// </summary>
public sealed class ProjectMappingResolver
{
    private readonly List<CompiledMapping> _mappings;

    public ProjectMappingResolver(IEnumerable<ProjectMapping> mappings)
    {
        _mappings = mappings.Select(m => new CompiledMapping(m)).ToList();
    }

    /// <summary>
    /// Returns test project path(s) for a changed file, or empty if no match.
    /// </summary>
    /// <param name="changedFilePath">The changed file path to resolve.</param>
    /// <returns>List of test project paths (usually 0 or 1).</returns>
    public List<string> ResolveTestProjects(string changedFilePath)
    {
        var normalizedPath = changedFilePath.Replace('\\', '/');
        var results = new List<string>();

        foreach (var mapping in _mappings)
        {
            var testProject = mapping.TryMatch(normalizedPath);
            if (testProject != null && !results.Contains(testProject))
            {
                results.Add(testProject);
            }
        }

        return results;
    }

    /// <summary>
    /// Batch resolution - returns unique test projects for all changed files.
    /// </summary>
    /// <param name="changedFiles">The changed files to resolve.</param>
    /// <returns>Unique list of test project paths.</returns>
    public List<string> ResolveAllTestProjects(IEnumerable<string> changedFiles)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in changedFiles)
        {
            var testProjects = ResolveTestProjects(file);
            foreach (var project in testProjects)
            {
                results.Add(project);
            }
        }

        return results.ToList();
    }

    /// <summary>
    /// Check if a file matches any projectMapping pattern.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file matches any mapping.</returns>
    public bool Matches(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var mapping in _mappings)
        {
            if (mapping.Matches(normalizedPath))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the number of configured mappings.
    /// </summary>
    public int MappingCount => _mappings.Count;

    private sealed class CompiledMapping
    {
        private readonly Regex _sourceRegex;
        private readonly string _testPattern;
        private readonly Matcher _excludeMatcher;
        private readonly bool _hasCapture;

        public CompiledMapping(ProjectMapping mapping)
        {
            _testPattern = mapping.TestPattern;
            _hasCapture = mapping.SourcePattern.Contains("{name}");

            // Convert glob pattern with {name} to regex
            // e.g., "src/Components/{name}/**" -> "^src/Components/(?<name>[^/]+)/.*$"
            var regexPattern = ConvertGlobToRegex(mapping.SourcePattern);
            _sourceRegex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Build exclude matcher
            _excludeMatcher = new Matcher();
            foreach (var exclude in mapping.Exclude)
            {
                _excludeMatcher.AddInclude(exclude);
            }
        }

        public string? TryMatch(string filePath)
        {
            // Check excludes first
            if (_excludeMatcher.Match(filePath).HasMatches)
            {
                return null;
            }

            var match = _sourceRegex.Match(filePath);
            if (!match.Success)
            {
                return null;
            }

            if (_hasCapture)
            {
                var nameGroup = match.Groups["name"];
                if (!nameGroup.Success)
                {
                    return null;
                }

                // Substitute {name} in test pattern
                return _testPattern.Replace("{name}", nameGroup.Value);
            }

            return _testPattern;
        }

        public bool Matches(string filePath)
        {
            // Check excludes first
            if (_excludeMatcher.Match(filePath).HasMatches)
            {
                return false;
            }

            return _sourceRegex.IsMatch(filePath);
        }

        private static string ConvertGlobToRegex(string globPattern)
        {
            // Normalize path separators
            var pattern = globPattern.Replace('\\', '/');

            // Escape regex special characters (except * and ?)
            pattern = Regex.Escape(pattern);

            // Restore escaped glob patterns and convert to regex
            // Note: Regex.Escape escapes { but not }, so we check for both patterns
            // First, handle {name} capture group
            pattern = pattern.Replace("\\{name}", "(?<name>[^/]+)");
            pattern = pattern.Replace("\\{name\\}", "(?<name>[^/]+)"); // In case } is also escaped

            // Handle ** (match any path including separators)
            pattern = pattern.Replace("\\*\\*", ".*");

            // Handle * (match any characters except path separator)
            pattern = pattern.Replace("\\*", "[^/]*");

            // Handle ? (match single character)
            pattern = pattern.Replace("\\?", ".");

            return "^" + pattern + "$";
        }
    }
}
