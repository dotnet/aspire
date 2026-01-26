// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestSelector.Models;

namespace Aspire.TestSelector.Analyzers;

/// <summary>
/// Checks if NuGet-dependent tests should be triggered based on affected packable projects.
/// </summary>
public sealed class NuGetDependencyChecker
{
    private readonly TestProjectFilter _projectFilter;
    private readonly List<string> _nugetDependentTestProjects;

    /// <summary>
    /// Creates a new NuGetDependencyChecker.
    /// </summary>
    /// <param name="projectFilter">The project filter for checking IsPackable.</param>
    /// <param name="nugetDependentTestProjects">Test projects that require NuGet packages (RequiresNuGets=true).</param>
    public NuGetDependencyChecker(TestProjectFilter projectFilter, IEnumerable<string> nugetDependentTestProjects)
    {
        _projectFilter = projectFilter;
        _nugetDependentTestProjects = nugetDependentTestProjects.ToList();
    }

    /// <summary>
    /// Checks if any affected projects are packable, which should trigger NuGet-dependent tests.
    /// </summary>
    /// <param name="affectedProjects">List of affected project paths.</param>
    /// <returns>Information about NuGet-dependent tests.</returns>
    public NuGetDependentTestsInfo Check(IEnumerable<string> affectedProjects)
    {
        var packableProjects = _projectFilter.FilterPackableProjects(affectedProjects);

        if (packableProjects.Count == 0)
        {
            return new NuGetDependentTestsInfo
            {
                Triggered = false,
                Reason = "No packable projects affected"
            };
        }

        var projectNames = packableProjects
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .ToList();

        return new NuGetDependentTestsInfo
        {
            Triggered = true,
            Reason = $"IsPackable projects affected: {string.Join(", ", projectNames)}",
            AffectedPackableProjects = packableProjects,
            Projects = _nugetDependentTestProjects.ToList()
        };
    }

    /// <summary>
    /// Checks if any affected projects are packable, with detailed classification information.
    /// </summary>
    /// <param name="affectedProjects">List of affected project paths.</param>
    /// <returns>Detailed information about NuGet-dependent tests.</returns>
    public NuGetCheckResult CheckWithDetails(IEnumerable<string> affectedProjects)
    {
        var result = new NuGetCheckResult();
        var projectList = affectedProjects.ToList();

        foreach (var project in projectList)
        {
            var info = _projectFilter.GetProjectInfoWithReason(project);
            result.AllProjects.Add(info);

            if (info.IsPackable)
            {
                result.PackableProjects.Add(info);
            }
        }

        if (result.PackableProjects.Count == 0)
        {
            result.Triggered = false;
            result.Reason = "No packable projects affected";
        }
        else
        {
            result.Triggered = true;
            var projectNames = result.PackableProjects.Select(p => p.Name ?? Path.GetFileNameWithoutExtension(p.Path));
            result.Reason = $"IsPackable projects affected: {string.Join(", ", projectNames)}";
            result.TriggeredTestProjects = _nugetDependentTestProjects.ToList();
        }

        return result;
    }

    /// <summary>
    /// Gets the list of NuGet-dependent test projects.
    /// </summary>
    public IReadOnlyList<string> NuGetDependentTestProjects => _nugetDependentTestProjects;

    /// <summary>
    /// Creates a default NuGetDependencyChecker for the Aspire repository.
    /// </summary>
    /// <param name="projectFilter">The project filter.</param>
    /// <returns>A configured NuGetDependencyChecker.</returns>
    public static NuGetDependencyChecker CreateDefault(TestProjectFilter projectFilter)
    {
        // These are the test projects that require built NuGet packages
        var nugetDependentProjects = new[]
        {
            "tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj",
            "tests/Aspire.EndToEnd.Tests/Aspire.EndToEnd.Tests.csproj",
            "tests/Aspire.Cli.EndToEnd.Tests/Aspire.Cli.EndToEnd.Tests.csproj"
        };

        return new NuGetDependencyChecker(projectFilter, nugetDependentProjects);
    }
}

/// <summary>
/// Detailed result of NuGet dependency checking.
/// </summary>
public sealed class NuGetCheckResult
{
    /// <summary>
    /// Whether NuGet-dependent tests should be triggered.
    /// </summary>
    public bool Triggered { get; set; }

    /// <summary>
    /// Reason for the decision.
    /// </summary>
    public string Reason { get; set; } = "";

    /// <summary>
    /// All projects that were checked.
    /// </summary>
    public List<ProjectInfoWithReason> AllProjects { get; } = [];

    /// <summary>
    /// Projects that are packable.
    /// </summary>
    public List<ProjectInfoWithReason> PackableProjects { get; } = [];

    /// <summary>
    /// Test projects that will be triggered (if any).
    /// </summary>
    public List<string> TriggeredTestProjects { get; set; } = [];
}
