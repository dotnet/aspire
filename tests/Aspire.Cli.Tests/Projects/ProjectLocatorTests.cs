// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Cli.Tests.Projects;

public class ProjectLocatorTests
{
    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsIfExplicitProjectFileDoesNotExist()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        var projectFile = new FileInfo(Path.Combine(projectDirectory, "AppHost.csproj"));
     
        var runner = new TestDotNetCliRunner();
        var projectLocator = new ProjectLocator(logger, runner, projectDirectory);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () => {
            await projectLocator.UseOrFindAppHostProjectFileAsync(projectFile);
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
        
        var runner = new TestDotNetCliRunner();
        
        var projectLocator = new ProjectLocator(logger, runner, projectDirectory);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () => {
            await projectLocator.UseOrFindAppHostProjectFileAsync(null);
        });

        Assert.Equal("Multiple project files found.", ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsIfNoProjectWasFound()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var tempDirectory = Path.GetTempPath();
        var projectDirectory = Path.Combine(tempDirectory, "Aspire.Cli.Tests", "Projects", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        
        var runner = new TestDotNetCliRunner();
        var projectLocator = new ProjectLocator(logger, runner, projectDirectory);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () =>{
            await projectLocator.UseOrFindAppHostProjectFileAsync(null);
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

        var runner = new TestDotNetCliRunner();
        var projectLocator = new ProjectLocator(logger, runner, projectDirectory);

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(projectFile);

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

        var runner = new TestDotNetCliRunner();
        var projectLocator = new ProjectLocator(logger, runner, projectDirectory);

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(null);
        Assert.Equal(projectFile.FullName, returnedProjectFile!.FullName);
    }
}