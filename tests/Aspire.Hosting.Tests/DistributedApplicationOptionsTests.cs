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
}
