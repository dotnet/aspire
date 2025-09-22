// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests;

public class ProjectFileNamingTests
{
    [Fact]
    public void SimpleTestThatShouldWork()
    {
        // Basic test to verify the test discovery works
        Assert.True(true);
    }

    [Fact]
    public void AllProjectFilesHaveUniqueNames()
    {
        // Get the repository root using existing utility
        var repoRoot = MSBuildUtils.GetRepoRoot();
        
        // Find all .csproj files in the repository
        var projectFiles = Directory.EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(ShouldCheckProject)  // Filter out expected duplicates
            .Select(path => new { Path = path, Name = Path.GetFileName(path) })
            .ToList();

        Assert.True(projectFiles.Count > 0, "Expected to find at least one .csproj file in the repository");

        // Group by file name and find duplicates
        var duplicateGroups = projectFiles
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Count > 0)
        {
            var duplicateDetails = duplicateGroups
                .Select(g => $"'{g.Key}' appears {g.Count()} times:{Environment.NewLine}" +
                           string.Join(Environment.NewLine, g.Select(p => $"  - {p.Path}")))
                .ToList();

            var errorMessage = $"Found {duplicateGroups.Count} project file name conflicts. " +
                             $"Each .csproj file must have a unique name to prevent build output conflicts:{Environment.NewLine}{Environment.NewLine}" +
                             string.Join($"{Environment.NewLine}{Environment.NewLine}", duplicateDetails);

            Assert.Fail(errorMessage);
        }
    }

    /// <summary>
    /// Determines if a project file should be checked for uniqueness.
    /// Excludes template files and build artifacts that are expected to have duplicates.
    /// </summary>
    private static bool ShouldCheckProject(string projectPath)
    {
        var normalizedPath = projectPath.Replace('\\', '/');
        
        // Exclude project template files - these are intentionally duplicated for different framework versions
        if (normalizedPath.Contains("/templates/"))
        {
            return false;
        }
        
        // Exclude build artifacts and intermediate output
        if (normalizedPath.Contains("/artifacts/") || 
            normalizedPath.Contains("/bin/") || 
            normalizedPath.Contains("/obj/"))
        {
            return false;
        }
        
        return true;
    }
}