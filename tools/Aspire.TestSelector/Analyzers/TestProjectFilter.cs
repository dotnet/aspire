// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Filters projects to identify test projects based on MSBuild properties.
/// Uses MSBuild API for accurate property evaluation.
/// </summary>
public sealed class TestProjectFilter
{
    private readonly string _repositoryRoot;
    private readonly MSBuildProjectEvaluator _evaluator;
    private readonly Dictionary<string, ProjectInfo> _projectCache = [];

    public TestProjectFilter(string repositoryRoot, MSBuildProjectEvaluator evaluator)
    {
        _repositoryRoot = repositoryRoot;
        _evaluator = evaluator;
    }

    /// <summary>
    /// Checks if a project is a test project (IsTestProject=true).
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
    /// Splits projects into test and non-test projects with detailed classification info.
    /// </summary>
    /// <param name="projectPaths">List of project paths.</param>
    /// <returns>Detailed split result.</returns>
    public ProjectSplitResult SplitProjectsWithDetails(IEnumerable<string> projectPaths)
    {
        var result = new ProjectSplitResult();

        foreach (var path in projectPaths)
        {
            var info = GetProjectInfoWithReason(path);
            if (info.IsTestProject)
            {
                result.TestProjects.Add(info);
            }
            else
            {
                result.SourceProjects.Add(info);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets detailed info about a project including the reason for its classification.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Project information with classification reason.</returns>
    public ProjectInfoWithReason GetProjectInfoWithReason(string projectPath)
    {
        var normalizedPath = NormalizePath(projectPath);
        var info = new ProjectInfoWithReason { Path = projectPath };

        if (!File.Exists(normalizedPath))
        {
            // Assume it's a test project if it's in the tests directory
            info.IsTestProject = projectPath.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
                                 projectPath.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase);
            info.ClassificationReason = info.IsTestProject
                ? "Path contains '/tests/' (file not found)"
                : "Path does not contain '/tests/' (file not found)";
            return info;
        }

        try
        {
            // Use MSBuild API for accurate property evaluation
            var properties = _evaluator.GetPropertyValues(projectPath, "IsTestProject", "IsPackable");

            // If all properties are null, MSBuild evaluation likely failed - use fallback
            if (properties.Values.All(v => v is null))
            {
                throw new InvalidOperationException("MSBuild evaluation returned no properties");
            }

            var isTestProjectValue = properties["IsTestProject"];
            var isPackableValue = properties["IsPackable"];

            info.IsTestProject = string.Equals(isTestProjectValue, "true", StringComparison.OrdinalIgnoreCase);
            info.IsPackable = string.Equals(isPackableValue, "true", StringComparison.OrdinalIgnoreCase);
            info.Name = Path.GetFileNameWithoutExtension(projectPath);

            info.ClassificationReason = string.IsNullOrEmpty(isTestProjectValue)
                ? "IsTestProject property not set (MSBuild evaluation)"
                : $"IsTestProject={isTestProjectValue} (MSBuild evaluation)";
        }
        catch (Exception ex)
        {
            // If MSBuild evaluation fails, make a best guess based on path
            info.IsTestProject = projectPath.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
                                 projectPath.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase) ||
                                 projectPath.EndsWith(".Tests.csproj", StringComparison.OrdinalIgnoreCase);
            info.ClassificationReason = $"MSBuild evaluation failed ({ex.Message}), guessed based on path";
        }

        return info;
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

        var info = EvaluateProjectInfo(normalizedPath);
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

    private ProjectInfo EvaluateProjectInfo(string projectPath)
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
            // Use MSBuild API for accurate property evaluation
            var properties = _evaluator.GetPropertyValues(projectPath, "IsTestProject", "IsPackable");

            // If all properties are null, MSBuild evaluation likely failed - use fallback
            if (properties.Values.All(v => v is null))
            {
                throw new InvalidOperationException("MSBuild evaluation returned no properties");
            }

            info.IsTestProject = string.Equals(properties["IsTestProject"], "true", StringComparison.OrdinalIgnoreCase);
            info.IsPackable = string.Equals(properties["IsPackable"], "true", StringComparison.OrdinalIgnoreCase);
            info.Name = Path.GetFileNameWithoutExtension(projectPath);
        }
        catch (Exception)
        {
            // If MSBuild evaluation fails, make a best guess based on path
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

/// <summary>
/// Result of splitting projects with detailed classification information.
/// </summary>
public sealed class ProjectSplitResult
{
    /// <summary>
    /// Test projects with classification details.
    /// </summary>
    public List<ProjectInfoWithReason> TestProjects { get; } = [];

    /// <summary>
    /// Source projects with classification details.
    /// </summary>
    public List<ProjectInfoWithReason> SourceProjects { get; } = [];
}

/// <summary>
/// Extended project information including the reason for classification.
/// </summary>
public sealed class ProjectInfoWithReason
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

    /// <summary>
    /// Human-readable explanation for why this project was classified as test/source.
    /// </summary>
    public string ClassificationReason { get; set; } = "";
}
