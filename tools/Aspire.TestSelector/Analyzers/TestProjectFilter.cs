// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Filters projects to identify test projects based on MSBuild properties.
/// </summary>
public sealed class TestProjectFilter
{
    private readonly string _repositoryRoot;
    private readonly Dictionary<string, ProjectInfo> _projectCache = [];

    public TestProjectFilter(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
    }

    /// <summary>
    /// Checks if a project is a test project (IsTestProject=true or has test SDK reference).
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the project is a test project.</returns>
    public bool IsTestProject(string projectPath)
    {
        var info = GetProjectInfo(projectPath);
        return info.IsTestProject;
    }

    /// <summary>
    /// Checks if a project is packable (IsPackable=true).
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the project is packable.</returns>
    public bool IsPackable(string projectPath)
    {
        var info = GetProjectInfo(projectPath);
        return info.IsPackable;
    }

    /// <summary>
    /// Filters a list of projects to only test projects.
    /// </summary>
    /// <param name="projectPaths">List of project paths.</param>
    /// <returns>List of test project paths.</returns>
    public List<string> FilterTestProjects(IEnumerable<string> projectPaths)
    {
        return projectPaths.Where(IsTestProject).ToList();
    }

    /// <summary>
    /// Filters a list of projects to only packable projects.
    /// </summary>
    /// <param name="projectPaths">List of project paths.</param>
    /// <returns>List of packable project paths.</returns>
    public List<string> FilterPackableProjects(IEnumerable<string> projectPaths)
    {
        return projectPaths.Where(IsPackable).ToList();
    }

    /// <summary>
    /// Splits projects into test and non-test projects.
    /// </summary>
    /// <param name="projectPaths">List of project paths.</param>
    /// <returns>A tuple of (testProjects, sourceProjects).</returns>
    public (List<string> TestProjects, List<string> SourceProjects) SplitProjects(IEnumerable<string> projectPaths)
    {
        var testProjects = new List<string>();
        var sourceProjects = new List<string>();

        foreach (var path in projectPaths)
        {
            if (IsTestProject(path))
            {
                testProjects.Add(path);
            }
            else
            {
                sourceProjects.Add(path);
            }
        }

        return (testProjects, sourceProjects);
    }

    /// <summary>
    /// Gets detailed info about a project.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Project information.</returns>
    public ProjectInfo GetProjectInfo(string projectPath)
    {
        var normalizedPath = NormalizePath(projectPath);

        if (_projectCache.TryGetValue(normalizedPath, out var cached))
        {
            return cached;
        }

        var info = ParseProjectFile(normalizedPath);
        _projectCache[normalizedPath] = info;
        return info;
    }

    private string NormalizePath(string projectPath)
    {
        // Convert to absolute path if relative
        if (!Path.IsPathRooted(projectPath))
        {
            projectPath = Path.Combine(_repositoryRoot, projectPath);
        }

        return Path.GetFullPath(projectPath);
    }

    private static ProjectInfo ParseProjectFile(string projectPath)
    {
        var info = new ProjectInfo { Path = projectPath };

        if (!File.Exists(projectPath))
        {
            // Assume it's a test project if it's in the tests directory
            info.IsTestProject = projectPath.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
                                 projectPath.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase);
            return info;
        }

        try
        {
            var doc = XDocument.Load(projectPath);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Check for explicit IsTestProject property
            var isTestProjectProp = doc.Descendants(ns + "IsTestProject").FirstOrDefault();
            if (isTestProjectProp != null)
            {
                info.IsTestProject = bool.TryParse(isTestProjectProp.Value, out var isTest) && isTest;
            }

            // Check for explicit IsPackable property
            var isPackableProp = doc.Descendants(ns + "IsPackable").FirstOrDefault();
            if (isPackableProp != null)
            {
                info.IsPackable = bool.TryParse(isPackableProp.Value, out var isPackable) && isPackable;
            }

            // Check for test SDK references (indicates test project even without explicit property)
            var sdkRefs = doc.Descendants(ns + "PackageReference")
                .Where(p => p.Attribute("Include")?.Value?.StartsWith("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) == true ||
                           p.Attribute("Include")?.Value?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true ||
                           p.Attribute("Include")?.Value?.StartsWith("NUnit", StringComparison.OrdinalIgnoreCase) == true ||
                           p.Attribute("Include")?.Value?.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase) == true);

            if (sdkRefs.Any() && !info.IsTestProject)
            {
                info.IsTestProject = true;
            }

            // Default IsPackable for projects without explicit property
            // Projects in src/ are typically packable, projects in tests/ are not
            if (isPackableProp == null)
            {
                info.IsPackable = !info.IsTestProject &&
                                  (projectPath.Contains("/src/", StringComparison.OrdinalIgnoreCase) ||
                                   projectPath.Contains("\\src\\", StringComparison.OrdinalIgnoreCase));
            }

            // Get project name
            info.Name = Path.GetFileNameWithoutExtension(projectPath);
        }
        catch (Exception)
        {
            // If we can't parse the project, make a best guess based on path
            info.IsTestProject = projectPath.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
                                 projectPath.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase) ||
                                 projectPath.EndsWith(".Tests.csproj", StringComparison.OrdinalIgnoreCase);
        }

        return info;
    }

    /// <summary>
    /// Clears the project cache.
    /// </summary>
    public void ClearCache()
    {
        _projectCache.Clear();
    }
}

/// <summary>
/// Information about a project.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    /// Full path to the project file.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Project name (without extension).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether the project is a test project.
    /// </summary>
    public bool IsTestProject { get; set; }

    /// <summary>
    /// Whether the project is packable (produces a NuGet package).
    /// </summary>
    public bool IsPackable { get; set; }
}
