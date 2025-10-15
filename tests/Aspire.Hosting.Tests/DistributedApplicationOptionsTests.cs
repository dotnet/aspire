// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests;

public class DistributedApplicationOptionsTests
{
    [Fact]
    public void ProjectDirectory_CanBeSetAndRetrieved()
    {
        var options = new DistributedApplicationOptions();
        var projectDirectory = "/path/to/project";

        options.ProjectDirectory = projectDirectory;

        Assert.Equal(projectDirectory, options.ProjectDirectory);
    }

    [Fact]
    public void ProjectDirectory_DefaultsToAssemblyMetadataWhenNotSet()
    {
        var options = new DistributedApplicationOptions();

        // When not explicitly set, ProjectDirectory falls back to assembly metadata resolution
        // In test context, this will resolve to a path (not null)
        Assert.NotNull(options.ProjectDirectory);
    }

    [Fact]
    public void ProjectDirectory_CanBeSetViaObjectInitializer()
    {
        var projectDirectory = "/path/to/delegated/app/project";
        var options = new DistributedApplicationOptions
        {
            ProjectDirectory = projectDirectory
        };

        Assert.Equal(projectDirectory, options.ProjectDirectory);
    }

    [Fact]
    public void ProjectDirectory_CanBeSetToNull()
    {
        var options = new DistributedApplicationOptions
        {
            ProjectDirectory = "/some/path"
        };

        options.ProjectDirectory = null;

        // Once explicitly set, the value is used even if it's null
        Assert.Null(options.ProjectDirectory);
    }

    [Fact]
    public void ProjectDirectory_IsUsedByBuilder()
    {
        var projectDirectory = OperatingSystem.IsWindows() ? @"C:\test\project" : "/test/project";
        var options = new DistributedApplicationOptions
        {
            ProjectDirectory = projectDirectory
        };

        var builder = DistributedApplication.CreateBuilder(options);

        Assert.Equal(projectDirectory, builder.AppHostDirectory);
    }

    [Fact]
    public void ProjectName_CanBeExplicitlySet()
    {
        // When ProjectName is explicitly set, it should be used regardless of assembly name or directory
        var projectDirectory = OperatingSystem.IsWindows() ? @"C:\projects\MyApp" : "/projects/MyApp";
        var options = new DistributedApplicationOptions
        {
            ProjectDirectory = projectDirectory,
            ProjectName = "CustomName"
        };

        var builder = (DistributedApplicationBuilder)DistributedApplication.CreateBuilder(options);

        // Should use the explicitly set name "CustomName"
        var expectedPath = Path.GetFullPath(Path.Join(projectDirectory, "CustomName"));
        Assert.Equal(expectedPath, builder.AppHostPath);
    }

    [Fact]
    public void ProjectName_UsesDirectoryNameWhenAppHostAssemblyNameIsAppHost()
    {
        // This test verifies the behavior when the entry assembly name is "apphost".
        // In real scenarios, single-file app hosts have an assembly name of "apphost".
        // This test cannot easily simulate that, but we verify the ProjectDirectory is used
        // when ProjectName is not explicitly set and assembly metadata is absent.
        
        var projectDirectory = OperatingSystem.IsWindows() ? @"C:\projects\MyCustomApp" : "/projects/MyCustomApp";
        var options = new DistributedApplicationOptions
        {
            ProjectDirectory = projectDirectory
            // Not setting ProjectName, so it will resolve from assembly or use directory logic
        };

        var builder = (DistributedApplicationBuilder)DistributedApplication.CreateBuilder(options);

        // Verify that the builder uses the project directory
        Assert.Equal(projectDirectory, builder.AppHostDirectory);
        
        // AppHostPath should be based on AppHostDirectory and the resolved name
        // The name will either come from assembly metadata or the assembly name itself
        Assert.StartsWith(projectDirectory, builder.AppHostPath);
    }
}
