// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Evaluates MSBuild projects to query properties like IsTestProject and RequiresNuGets.
/// Must call <see cref="Initialize"/> before any other methods.
/// </summary>
public sealed class MSBuildProjectEvaluator : IDisposable
{
    private static bool s_initialized;
    private static readonly object s_initLock = new();

    private readonly ProjectCollection _projectCollection;
    private readonly Dictionary<string, Project> _projectCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _repositoryRoot;
    private bool _disposed;

    /// <summary>
    /// Initializes the MSBuild locator. Must be called once before loading any MSBuild types.
    /// This should be called at application startup, before any code that references Microsoft.Build types.
    /// </summary>
    public static void Initialize()
    {
        lock (s_initLock)
        {
            if (s_initialized)
            {
                return;
            }

            // Register the default MSBuild instance (from the .NET SDK)
            MSBuildLocator.RegisterDefaults();
            s_initialized = true;
        }
    }

    /// <summary>
    /// Creates a new MSBuildProjectEvaluator.
    /// </summary>
    /// <param name="repositoryRoot">The repository root path for resolving relative paths.</param>
    public MSBuildProjectEvaluator(string repositoryRoot)
    {
        if (!s_initialized)
        {
            throw new InvalidOperationException(
                "MSBuildProjectEvaluator.Initialize() must be called before creating an instance.");
        }

        _repositoryRoot = repositoryRoot;
        _projectCollection = new ProjectCollection();
    }

    /// <summary>
    /// Gets a property value from a project.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file (absolute or relative to repository root).</param>
    /// <param name="propertyName">The MSBuild property name to query.</param>
    /// <returns>The evaluated property value, or null if not set.</returns>
    public string? GetPropertyValue(string projectPath, string propertyName)
    {
        var project = LoadProject(projectPath);
        return project?.GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Gets the IsTestProject property value.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if IsTestProject is "true", false otherwise.</returns>
    public bool IsTestProject(string projectPath)
    {
        var value = GetPropertyValue(projectPath, "IsTestProject");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the RequiresNuGets property value.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if RequiresNuGets is "true", false otherwise.</returns>
    public bool RequiresNuGets(string projectPath)
    {
        var value = GetPropertyValue(projectPath, "RequiresNuGets");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the IsPackable property value.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if IsPackable is "true", false otherwise.</returns>
    public bool IsPackable(string projectPath)
    {
        var value = GetPropertyValue(projectPath, "IsPackable");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Evaluates multiple properties from a project in a single operation.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="propertyNames">Property names to query.</param>
    /// <returns>Dictionary of property names to evaluated values.</returns>
    public Dictionary<string, string?> GetPropertyValues(string projectPath, params string[] propertyNames)
    {
        var project = LoadProject(projectPath);
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var propertyName in propertyNames)
        {
            result[propertyName] = project?.GetPropertyValue(propertyName);
        }

        return result;
    }

    /// <summary>
    /// Finds all test projects in the given directory that have RequiresNuGets=true.
    /// </summary>
    /// <param name="searchDirectory">Directory to search (defaults to tests/ under repository root).</param>
    /// <returns>List of project paths that require NuGets.</returns>
    public List<string> FindProjectsRequiringNuGets(string? searchDirectory = null)
    {
        searchDirectory ??= Path.Combine(_repositoryRoot, "tests");

        if (!Directory.Exists(searchDirectory))
        {
            return [];
        }

        var result = new List<string>();
        var projectFiles = Directory.GetFiles(searchDirectory, "*.csproj", SearchOption.AllDirectories);

        foreach (var projectFile in projectFiles)
        {
            if (RequiresNuGets(projectFile))
            {
                // Return relative path from repository root
                var relativePath = Path.GetRelativePath(_repositoryRoot, projectFile)
                    .Replace('\\', '/');
                result.Add(relativePath);
            }
        }

        return result;
    }

    private Project? LoadProject(string projectPath)
    {
        var normalizedPath = NormalizePath(projectPath);

        if (_projectCache.TryGetValue(normalizedPath, out var cached))
        {
            return cached;
        }

        if (!File.Exists(normalizedPath))
        {
            return null;
        }

        try
        {
            var project = _projectCollection.LoadProject(normalizedPath);
            _projectCache[normalizedPath] = project;
            return project;
        }
        catch (Exception)
        {
            // If we can't load the project, return null and let caller handle fallback
            return null;
        }
    }

    private string NormalizePath(string projectPath)
    {
        if (!Path.IsPathRooted(projectPath))
        {
            projectPath = Path.Combine(_repositoryRoot, projectPath);
        }

        return Path.GetFullPath(projectPath);
    }

    /// <summary>
    /// Clears the project cache.
    /// </summary>
    public void ClearCache()
    {
        foreach (var project in _projectCache.Values)
        {
            _projectCollection.UnloadProject(project);
        }
        _projectCache.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearCache();
        _projectCollection.Dispose();
        _disposed = true;
    }
}
