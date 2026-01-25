// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Infrastructure.Tests.Helpers;

/// <summary>
/// Provides project filtering logic for test enumeration.
/// Reimplements the logic from Test-EnumerateTestsFiltering.ps1.
/// </summary>
public static class ProjectFilter
{
    /// <summary>
    /// Converts a project path to its shortname.
    /// For example: "tests/Aspire.Milvus.Client.Tests/" -> "Milvus.Client"
    /// </summary>
    /// <param name="projectPath">The project path (e.g., "tests/Aspire.Milvus.Client.Tests/").</param>
    /// <returns>The shortname (e.g., "Milvus.Client").</returns>
    public static string ConvertProjectPathToShortname(string projectPath)
    {
        // Extract directory name from path (handle trailing slash)
        var path = projectPath.TrimEnd('/');
        var dirName = path.Split('/').Last();

        // Convert to shortname: Aspire.Milvus.Client.Tests -> Milvus.Client
        var shortname = dirName;

        // Remove "Aspire." prefix if present
        if (shortname.StartsWith("Aspire.", StringComparison.Ordinal))
        {
            shortname = shortname.Substring(7); // "Aspire.".Length
        }

        // Remove ".Tests" suffix if present
        if (shortname.EndsWith(".Tests", StringComparison.Ordinal))
        {
            shortname = shortname.Substring(0, shortname.Length - 6); // ".Tests".Length
        }

        return shortname;
    }

    /// <summary>
    /// Applies the projects filter to a list of test shortnames.
    /// </summary>
    /// <param name="allTests">List of all test shortnames.</param>
    /// <param name="projectsFilterJson">JSON array of project paths to filter by.</param>
    /// <returns>Filtered list of test shortnames.</returns>
    public static IEnumerable<string> ApplyProjectsFilter(IEnumerable<string> allTests, string? projectsFilterJson)
    {
        var testsList = allTests.ToList();

        if (string.IsNullOrEmpty(projectsFilterJson) || projectsFilterJson == "[]")
        {
            return testsList;
        }

        List<string>? projects;
        try
        {
            projects = JsonSerializer.Deserialize<List<string>>(projectsFilterJson);
        }
        catch (JsonException)
        {
            return testsList;
        }

        if (projects == null || projects.Count == 0)
        {
            return testsList;
        }

        var allowedShortnames = projects
            .Select(ConvertProjectPathToShortname)
            .ToHashSet(StringComparer.Ordinal);

        if (allowedShortnames.Count == 0)
        {
            return testsList;
        }

        return testsList.Where(allowedShortnames.Contains);
    }

    /// <summary>
    /// Applies the projects filter to a list of test shortnames.
    /// </summary>
    /// <param name="allTests">List of all test shortnames.</param>
    /// <param name="projects">List of project paths to filter by.</param>
    /// <returns>Filtered list of test shortnames.</returns>
    public static IEnumerable<string> ApplyProjectsFilter(IEnumerable<string> allTests, IEnumerable<string>? projects)
    {
        var testsList = allTests.ToList();

        if (projects == null)
        {
            return testsList;
        }

        var projectsList = projects.ToList();
        if (projectsList.Count == 0)
        {
            return testsList;
        }

        var allowedShortnames = projectsList
            .Select(ConvertProjectPathToShortname)
            .ToHashSet(StringComparer.Ordinal);

        if (allowedShortnames.Count == 0)
        {
            return testsList;
        }

        return testsList.Where(allowedShortnames.Contains);
    }
}
