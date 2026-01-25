// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Aspire.TestSelector;

/// <summary>
/// Maps files to test categories based on triggerPaths configuration.
/// </summary>
public sealed class CategoryMapper
{
    private readonly Dictionary<string, CategoryConfig> _categoryConfigs;
    private readonly Dictionary<string, CompiledCategory> _compiledCategories;

    public CategoryMapper(Dictionary<string, CategoryConfig> categoryConfigs)
    {
        _categoryConfigs = categoryConfigs;
        _compiledCategories = categoryConfigs
            .ToDictionary(
                kvp => kvp.Key,
                kvp => new CompiledCategory(kvp.Value));
    }

    /// <summary>
    /// Check if a file triggers a category (matches triggerPaths but not excludePaths).
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="categoryName">The category name to check against.</param>
    /// <returns>True if the file triggers the category.</returns>
    public bool FileTriggersCategory(string filePath, string categoryName)
    {
        if (!_compiledCategories.TryGetValue(categoryName, out var compiled))
        {
            return false;
        }

        return compiled.Matches(filePath);
    }

    /// <summary>
    /// Gets all categories triggered by a set of files, and which files matched.
    /// </summary>
    /// <param name="files">The files to check.</param>
    /// <returns>A tuple of (Categories dictionary, set of files that matched any category).</returns>
    public (Dictionary<string, bool> Categories, HashSet<string> MatchedFiles) GetCategoriesTriggeredByFiles(IEnumerable<string> files)
    {
        var categories = new Dictionary<string, bool>();
        var matchedFiles = new HashSet<string>();

        // Initialize all categories to false
        foreach (var categoryName in _categoryConfigs.Keys)
        {
            categories[categoryName] = false;
        }

        // Check each file against each category
        foreach (var file in files)
        {
            var normalizedFile = file.Replace('\\', '/');
            var fileMatched = false;

            foreach (var (categoryName, compiled) in _compiledCategories)
            {
                if (compiled.Matches(normalizedFile))
                {
                    categories[categoryName] = true;
                    fileMatched = true;
                }
            }

            if (fileMatched)
            {
                matchedFiles.Add(normalizedFile);
            }
        }

        return (categories, matchedFiles);
    }

    /// <summary>
    /// Gets all categories triggered by a list of files.
    /// </summary>
    /// <param name="files">The files to check.</param>
    /// <returns>Dictionary mapping category names to whether they're triggered.</returns>
    public Dictionary<string, bool> GetTriggeredCategories(IEnumerable<string> files)
    {
        var (categories, _) = GetCategoriesTriggeredByFiles(files);
        return categories;
    }

    /// <summary>
    /// Gets all configured categories.
    /// </summary>
    public IEnumerable<string> GetAllCategories()
    {
        return _categoryConfigs.Keys;
    }

    /// <summary>
    /// Checks if a category has the triggerAll flag set.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>True if the category triggers all tests when matched.</returns>
    public bool IsTriggerAllCategory(string categoryName)
    {
        return _categoryConfigs.TryGetValue(categoryName, out var config) && config.TriggerAll;
    }

    /// <summary>
    /// Gets the category configuration for a specific category.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>The category configuration, or null if not found.</returns>
    public CategoryConfig? GetCategoryConfig(string categoryName)
    {
        return _categoryConfigs.TryGetValue(categoryName, out var config) ? config : null;
    }

    private sealed class CompiledCategory
    {
        private readonly Matcher _triggerMatcher;
        private readonly Matcher _excludeMatcher;

        public CompiledCategory(CategoryConfig config)
        {
            _triggerMatcher = new Matcher();
            foreach (var pattern in config.TriggerPaths)
            {
                _triggerMatcher.AddInclude(pattern);
            }

            _excludeMatcher = new Matcher();
            foreach (var pattern in config.ExcludePaths)
            {
                _excludeMatcher.AddInclude(pattern);
            }
        }

        public bool Matches(string filePath)
        {
            var normalizedPath = filePath.Replace('\\', '/');

            // Check excludes first
            if (_excludeMatcher.Match(normalizedPath).HasMatches)
            {
                return false;
            }

            return _triggerMatcher.Match(normalizedPath).HasMatches;
        }
    }
}
