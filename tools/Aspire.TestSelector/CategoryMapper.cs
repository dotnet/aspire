// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;

namespace Aspire.TestSelector;

/// <summary>
/// Maps test projects to their categories.
/// </summary>
public sealed class CategoryMapper
{
    private readonly Dictionary<string, CategoryConfig> _categoryConfigs;
    private readonly Dictionary<string, string> _projectToCategory;

    public CategoryMapper(Dictionary<string, CategoryConfig> categoryConfigs)
    {
        _categoryConfigs = categoryConfigs;
        _projectToCategory = BuildProjectToCategoryMap();
    }

    private Dictionary<string, string> BuildProjectToCategoryMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (categoryName, config) in _categoryConfigs)
        {
            if (!config.TestProjects.IsAuto)
            {
                foreach (var project in config.TestProjects.Projects)
                {
                    // Normalize project path for consistent matching
                    var normalizedPath = NormalizePath(project);
                    if (!map.ContainsKey(normalizedPath))
                    {
                        map[normalizedPath] = categoryName;
                    }
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Gets the category for a test project.
    /// </summary>
    /// <param name="projectPath">Path to the test project.</param>
    /// <returns>Category name or "integrations" if not explicitly mapped.</returns>
    public string GetCategoryForProject(string projectPath)
    {
        var normalizedPath = NormalizePath(projectPath);

        // Check exact match first
        if (_projectToCategory.TryGetValue(normalizedPath, out var category))
        {
            return category;
        }

        // Check if the path contains any of the configured project paths
        foreach (var (configuredPath, configuredCategory) in _projectToCategory)
        {
            if (normalizedPath.Contains(configuredPath, StringComparison.OrdinalIgnoreCase) ||
                configuredPath.Contains(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return configuredCategory;
            }
        }

        // Default to integrations for test projects not explicitly mapped
        return "integrations";
    }

    /// <summary>
    /// Gets all categories triggered by a list of test projects.
    /// </summary>
    /// <param name="testProjects">List of test project paths.</param>
    /// <returns>Dictionary mapping category names to whether they're triggered.</returns>
    public Dictionary<string, bool> GetTriggeredCategories(IEnumerable<string> testProjects)
    {
        var triggered = new Dictionary<string, bool>();

        // Initialize all categories to false
        foreach (var categoryName in _categoryConfigs.Keys)
        {
            triggered[categoryName] = false;
        }

        // Mark categories as triggered based on the test projects
        foreach (var project in testProjects)
        {
            var category = GetCategoryForProject(project);
            triggered[category] = true;
        }

        return triggered;
    }

    /// <summary>
    /// Groups test projects by their categories.
    /// </summary>
    /// <param name="testProjects">List of test project paths.</param>
    /// <returns>Dictionary mapping category names to their test projects.</returns>
    public Dictionary<string, List<string>> GroupByCategory(IEnumerable<string> testProjects)
    {
        var groups = new Dictionary<string, List<string>>();

        foreach (var project in testProjects)
        {
            var category = GetCategoryForProject(project);

            if (!groups.TryGetValue(category, out var list))
            {
                list = [];
                groups[category] = list;
            }

            list.Add(project);
        }

        return groups;
    }

    /// <summary>
    /// Gets all test projects for a specific category.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>List of test project paths for the category.</returns>
    public List<string> GetProjectsForCategory(string categoryName)
    {
        if (!_categoryConfigs.TryGetValue(categoryName, out var config))
        {
            return [];
        }

        if (config.TestProjects.IsAuto)
        {
            // For "auto" categories (like integrations), return empty
            // The caller should use the affected projects instead
            return [];
        }

        return config.TestProjects.Projects.ToList();
    }

    /// <summary>
    /// Gets all configured categories.
    /// </summary>
    public IEnumerable<string> GetAllCategories()
    {
        return _categoryConfigs.Keys;
    }

    /// <summary>
    /// Checks if a category uses auto-discovery.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>True if the category uses auto-discovery.</returns>
    public bool IsAutoCategory(string categoryName)
    {
        return _categoryConfigs.TryGetValue(categoryName, out var config) && config.TestProjects.IsAuto;
    }

    private static string NormalizePath(string path)
    {
        // Remove trailing slashes and normalize separators
        return path.TrimEnd('/', '\\').Replace('\\', '/');
    }
}
