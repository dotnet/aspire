// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Cli.Tests.Projects;

public class ProjectLocatorTests
{
    [Fact]
    public void UseOrFindAppHostProjectFileThrowsIfExplicitProjectFileDoesNotExist()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        var projectFile = new FileInfo(Path.Combine(projectDirectory, "AppHost.csproj"));
        var projectLocator = new ProjectLocator(logger, projectDirectory);

        var ex = Assert.Throws<ProjectLocatorException>(() =>{
            projectLocator.UseOrFindAppHostProjectFile(projectFile);
        });

        Assert.Equal("Project file does not exist.", ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsTwoProjectFilesFound()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        var projectFile1 = new FileInfo(Path.Combine(projectDirectory, "AppHost1.csproj"));
        await File.WriteAllTextAsync(projectFile1.FullName, "Not a real project file.");
        
        var projectFile2 = new FileInfo(Path.Combine(projectDirectory, "AppHost2.csproj"));
        await File.WriteAllTextAsync(projectFile2.FullName, "Not a real project file.");
        
        var projectLocator = new ProjectLocator(logger, projectDirectory);

        var ex = Assert.Throws<ProjectLocatorException>(() =>{
            projectLocator.UseOrFindAppHostProjectFile(null);
        });

        Assert.Equal("Multiple project files found.", ex.Message);
    }

    [Fact]
    public void UseOrFindAppHostProjectFileThrowsIfNoProjectWasFound()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        
        var projectLocator = new ProjectLocator(logger, projectDirectory);

        var ex = Assert.Throws<ProjectLocatorException>(() =>{
            projectLocator.UseOrFindAppHostProjectFile(null);
        });

        Assert.Equal("No project file found.", ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileReturnsExplicitProjectIfExistsAndProvided()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        var projectFile = new FileInfo(Path.Combine(projectDirectory, "MalformedProjectFile.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var projectLocator = new ProjectLocator(logger, projectDirectory);

        var returnedProjectFile = projectLocator.UseOrFindAppHostProjectFile(projectFile);

        Assert.Equal(projectFile, returnedProjectFile);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileReturnsProjectFileInDirectoryIfNotExplicitlyProvided()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        var projectFile = new FileInfo(Path.Combine(projectDirectory, "MalformedProjectFile.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var projectLocator = new ProjectLocator(logger, projectDirectory);

        var returnedProjectFile = projectLocator.UseOrFindAppHostProjectFile(null);
        Assert.Equal(projectFile.FullName, returnedProjectFile!.FullName);
    }
}