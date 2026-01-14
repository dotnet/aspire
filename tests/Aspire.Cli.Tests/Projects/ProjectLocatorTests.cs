// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class ProjectLocatorTests(ITestOutputHelper outputHelper)
{
    private static Aspire.Cli.CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory)
    {
        // NOTE: This would normally be in the users home directory, but for tests we create
        //       it in the temporary workspace directory.
        var settingsDirectory = workingDirectory.CreateSubdirectory(".aspire");
        var hivesDirectory = settingsDirectory.CreateSubdirectory("hives");
    var cacheDirectory = new DirectoryInfo(Path.Combine(workingDirectory.FullName, ".aspire", "cache"));
    return new CliExecutionContext(workingDirectory, hivesDirectory, cacheDirectory, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")));
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsIfExplicitProjectFileDoesNotExist()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () => {
            await projectLocator.UseOrFindAppHostProjectFileAsync(projectFile, createSettingsFile: true);
        });

        Assert.Equal(ErrorStrings.ProjectFileDoesntExist, ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileUsesAppHostSpecifiedInSettings()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var targetAppHostDirectory = workspace.WorkspaceRoot.CreateSubdirectory("TargetAppHost");
        var targetAppHostProjectFile = new FileInfo(Path.Combine(targetAppHostDirectory.FullName, "TargetAppHost.csproj"));
        await File.WriteAllTextAsync(targetAppHostProjectFile.FullName, "Not a real apphost");

        var otherAppHostDirectory = workspace.WorkspaceRoot.CreateSubdirectory("OtherAppHost");
        var otherAppHostProjectFile = new FileInfo(Path.Combine(otherAppHostDirectory.FullName, "OtherAppHost.csproj"));
        await File.WriteAllTextAsync(targetAppHostProjectFile.FullName, "Not a real apphost");

        var workspaceSettingsDirectory = workspace.CreateDirectory(".aspire");
        var aspireSettingsFile = new FileInfo(Path.Combine(workspaceSettingsDirectory.FullName, "settings.json"));

        using var writer = aspireSettingsFile.OpenWrite();
        await JsonSerializer.SerializeAsync(writer, new
        {
            appHostPath = Path.GetRelativePath(aspireSettingsFile.Directory!.FullName, targetAppHostProjectFile.FullName)
        });
        writer.Close();

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);

        Assert.Equal(targetAppHostProjectFile.FullName, foundAppHost?.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileUsesAppHostSpecifiedInSettingsWalksTree()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var dir1 = workspace.WorkspaceRoot.CreateSubdirectory("dir1");
        var dir2 = dir1.CreateSubdirectory("dir2");

        var targetAppHostDirectory = dir2.CreateSubdirectory("TargetAppHost");
        var targetAppHostProjectFile = new FileInfo(Path.Combine(targetAppHostDirectory.FullName, "TargetAppHost.csproj"));
        await File.WriteAllTextAsync(targetAppHostProjectFile.FullName, "Not a real apphost");

        var otherAppHostDirectory = workspace.WorkspaceRoot.CreateSubdirectory("OtherAppHost");
        var otherAppHostProjectFile = new FileInfo(Path.Combine(otherAppHostDirectory.FullName, "OtherAppHost.csproj"));
        await File.WriteAllTextAsync(targetAppHostProjectFile.FullName, "Not a real apphost");

        var workspaceSettingsDirectory = workspace.CreateDirectory(".aspire");
        var aspireSettingsFile = new FileInfo(Path.Combine(workspaceSettingsDirectory.FullName, "settings.json"));

        using var writer = aspireSettingsFile.OpenWrite();
        await JsonSerializer.SerializeAsync(writer, new
        {
            appHostPath = Path.GetRelativePath(aspireSettingsFile.Directory!.FullName, targetAppHostProjectFile.FullName)
        });
        writer.Close();

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);

        Assert.Equal(targetAppHostProjectFile.FullName, foundAppHost?.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileFallsBackWhenSettingsFileSpecifiesNonexistentAppHost()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a real apphost project file that can be discovered by scanning
        var realAppHostProjectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "RealAppHost.csproj"));
        await File.WriteAllTextAsync(realAppHostProjectFile.FullName, "Not a real apphost project");

        // Create settings file that points to a non-existent apphost file
        var workspaceSettingsDirectory = workspace.CreateDirectory(".aspire");
        var aspireSettingsFile = new FileInfo(Path.Combine(workspaceSettingsDirectory.FullName, "settings.json"));

        using var writer = aspireSettingsFile.OpenWrite();
        await JsonSerializer.SerializeAsync(writer, new
        {
            appHostPath = "NonexistentAppHost/NonexistentAppHost.csproj"
        });
        writer.Close();

        var projectFactory = new TestAppHostProjectFactory
        {
            ValidateAppHostCallback = projectFile =>
            {
                if (projectFile.FullName == realAppHostProjectFile.FullName)
                {
                    return new AppHostValidationResult(IsValid: true);
                }
                return new AppHostValidationResult(IsValid: false);
            }
        };

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());

        // This should fallback to scanning and find the real apphost project
        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);

        Assert.Equal(realAppHostProjectFile.FullName, foundAppHost?.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileNormalizesForwardSlashesInSettings()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var targetAppHostDirectory = workspace.WorkspaceRoot.CreateSubdirectory("TargetAppHost");
        var targetAppHostProjectFile = new FileInfo(Path.Combine(targetAppHostDirectory.FullName, "TargetAppHost.csproj"));
        await File.WriteAllTextAsync(targetAppHostProjectFile.FullName, "Not a real apphost");

        var workspaceSettingsDirectory = workspace.CreateDirectory(".aspire");
        var aspireSettingsFile = new FileInfo(Path.Combine(workspaceSettingsDirectory.FullName, "settings.json"));

        // Get the relative path and ensure it uses forward slashes (as stored in settings.json)
        var relativePath = Path.GetRelativePath(aspireSettingsFile.Directory!.FullName, targetAppHostProjectFile.FullName);
        var forwardSlashPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

        using var writer = aspireSettingsFile.OpenWrite();
        await JsonSerializer.SerializeAsync(writer, new
        {
            appHostPath = forwardSlashPath
        });
        writer.Close();

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);

        // Should successfully find the file even though the path in settings uses forward slashes
        Assert.Equal(targetAppHostProjectFile.FullName, foundAppHost?.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFilePromptsWhenMultipleFilesFound()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile1 = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost1.csproj"));
        await File.WriteAllTextAsync(projectFile1.FullName, "Not a real project file.");

        var projectFile2 = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost2.csproj"));
        await File.WriteAllTextAsync(projectFile2.FullName, "Not a real project file.");

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var selectedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);

        Assert.Equal(projectFile1.FullName, selectedProjectFile!.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileOnlyConsidersValidAppHostProjects()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProject.FullName, "Not a real apphost project.");

        var webProject = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "WebProject.csproj"));
        await File.WriteAllTextAsync(webProject.FullName, "Not a real web project.");

        var projectFactory = new TestAppHostProjectFactory
        {
            ValidateAppHostCallback = projectFile =>
            {
                if (projectFile.FullName == appHostProject.FullName)
                {
                    return new AppHostValidationResult(IsValid: true);
                }
                return new AppHostValidationResult(IsValid: false);
            }
        };

        var interactionService = new TestConsoleInteractionService();

        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());
        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);
        Assert.Equal(appHostProject.FullName, foundAppHost?.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsIfNoProjectWasFound()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var interactionService = new TestConsoleInteractionService();

        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () =>{
            await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);
        });

        Assert.Equal(ErrorStrings.NoProjectFileFound, ex.Message);
    }

    [Theory]
    [InlineData(".csproj")]
    [InlineData(".fsproj")]
    [InlineData(".vbproj")]
    public async Task UseOrFindAppHostProjectFileReturnsExplicitProjectIfExistsAndProvided(string projectFileExtension)
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, $"AppHost{projectFileExtension}"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(projectFile, createSettingsFile: true);

        Assert.Equal(projectFile, returnedProjectFile);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileReturnsProjectFileInDirectoryIfNotExplicitlyProvided()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var interactionService = new TestConsoleInteractionService();

        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true);
        Assert.Equal(projectFile.FullName, returnedProjectFile!.FullName);
    }

    [Fact]
    public async Task CreateSettingsFileIfNotExistsAsync_UsesForwardSlashPathSeparator()
    {
        // Arrange
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var srcDirectory = workspace.CreateDirectory("src");
        var appHostDirectory = srcDirectory.CreateSubdirectory("AppHost");
        var appHostProjectFile = new FileInfo(Path.Combine(appHostDirectory.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(appHostProjectFile.FullName, "Not a real project file.");

        var projectFactory = new TestAppHostProjectFactory
        {
            ValidateAppHostCallback = _ => new AppHostValidationResult(IsValid: true)
        };

        var interactionService = new TestConsoleInteractionService();

        // Simulated global settings path for test isolation.
        var globalSettingsFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.global.json");
        var globalSettingsFile = new FileInfo(globalSettingsFilePath);

        var config = new ConfigurationBuilder().Build();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var configurationService = new ConfigurationService(config, executionContext, globalSettingsFile);

        var locator = new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());

        await locator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true, CancellationToken.None);

        var settingsFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json"));
        Assert.True(settingsFile.Exists, "Settings file should exist.");

        var settingsJson = await File.ReadAllTextAsync(settingsFile.FullName);
        var settings = JsonSerializer.Deserialize<CliSettings>(settingsJson);

        Assert.NotNull(settings);
        Assert.NotNull(settings!.AppHostPath);
        Assert.DoesNotContain('\\', settings.AppHostPath); // Ensure no backslashes
        Assert.Contains('/', settings.AppHostPath); // Ensure forward slashes
    }

    [Fact]
    public async Task FindAppHostProjectFilesAsync_DiscoversSingleFileAppHostInRootDirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a valid single-file apphost
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext);

        var foundFiles = await projectLocator.FindAppHostProjectFilesAsync(workspace.WorkspaceRoot.FullName, CancellationToken.None);

        Assert.Single(foundFiles);
        Assert.Equal(appHostFile.FullName, foundFiles[0].FullName);
    }

    [Fact]
    public async Task FindAppHostProjectFilesAsync_DiscoversSingleFileAppHostInSubdirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var subDir = workspace.WorkspaceRoot.CreateSubdirectory("SubProject");
        var appHostFile = new FileInfo(Path.Combine(subDir.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext);

        var foundFiles = await projectLocator.FindAppHostProjectFilesAsync(workspace.WorkspaceRoot.FullName, CancellationToken.None);

        Assert.Single(foundFiles);
        Assert.Equal(appHostFile.FullName, foundFiles[0].FullName);
    }

    [Fact]
    public async Task FindAppHostProjectFilesAsync_IgnoresSingleFileAppHostWhenSiblingCsprojExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a subdirectory with both apphost.cs and a .csproj file (single-file apphost should be ignored)
        var dirWithBoth = workspace.WorkspaceRoot.CreateSubdirectory("WithBoth");
        var appHostFile = new FileInfo(Path.Combine(dirWithBoth.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var csprojFile = new FileInfo(Path.Combine(dirWithBoth.FullName, "RegularProject.csproj"));
        await File.WriteAllTextAsync(csprojFile.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        // Create another subdirectory with only apphost.cs (single-file apphost should be found)
        var dirWithOnlyAppHost = workspace.WorkspaceRoot.CreateSubdirectory("OnlyAppHost");
        var validAppHostFile = new FileInfo(Path.Combine(dirWithOnlyAppHost.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            validAppHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext, projectFile =>
        {
            // The .csproj in WithBoth is not an AppHost, but has sibling apphost.cs so is "possibly unbuildable"
            if (projectFile.FullName == csprojFile.FullName)
            {
                return new AppHostValidationResult(IsValid: false, IsPossiblyUnbuildable: true);
            }
            return new AppHostValidationResult(IsValid: true);
        });

        var foundFiles = await projectLocator.FindAppHostProjectFilesAsync(workspace.WorkspaceRoot.FullName, CancellationToken.None);

        // Should find the valid single-file apphost (from OnlyAppHost directory)
        // and the potentially unbuildable .csproj (from WithBoth directory due to sibling apphost.cs)
        // but NOT the single-file apphost from WithBoth directory (ignored due to sibling .csproj)
        Assert.Equal(2, foundFiles.Count);

        var foundPaths = foundFiles.Select(f => f.FullName).ToHashSet();
        Assert.Contains(validAppHostFile.FullName, foundPaths);
        Assert.Contains(csprojFile.FullName, foundPaths);
        Assert.DoesNotContain(appHostFile.FullName, foundPaths);
    }

    [Fact]
    public async Task FindAppHostProjectFilesAsync_IgnoresSingleFileAppHostWithoutDirective()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create an apphost.cs file without the required directive
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, @"using Aspire.Hosting;
var builder = DistributedApplication.CreateBuilder(args);
builder.Build().Run();");

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext);

        var foundFiles = await projectLocator.FindAppHostProjectFilesAsync(workspace.WorkspaceRoot.FullName, CancellationToken.None);

        Assert.Empty(foundFiles);
    }

    [Fact]
    public async Task FindAppHostProjectFilesAsync_HandlesMixedAppHostAndSingleFile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a valid .csproj AppHost in subdirectory
        var subDir1 = workspace.WorkspaceRoot.CreateSubdirectory("ProjectAppHost");
        var csprojFile = new FileInfo(Path.Combine(subDir1.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(csprojFile.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        // Create a valid single-file AppHost in another subdirectory
        var subDir2 = workspace.WorkspaceRoot.CreateSubdirectory("SingleFileAppHost");
        var appHostFile = new FileInfo(Path.Combine(subDir2.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext, projectFile =>
        {
            if (projectFile.FullName == csprojFile.FullName)
            {
                return new AppHostValidationResult(IsValid: true);
            }
            return new AppHostValidationResult(IsValid: true);
        });

        var foundFiles = await projectLocator.FindAppHostProjectFilesAsync(workspace.WorkspaceRoot.FullName, CancellationToken.None);

        Assert.Equal(2, foundFiles.Count);
        // Verify deterministic ordering (sorted by FullName)
        Assert.True(foundFiles[0].FullName.CompareTo(foundFiles[1].FullName) < 0);

        var foundPaths = foundFiles.Select(f => f.FullName).ToHashSet();
        Assert.Contains(csprojFile.FullName, foundPaths);
        Assert.Contains(appHostFile.FullName, foundPaths);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAsync_AcceptsExplicitSingleFileAppHost()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext);

        var result = await projectLocator.UseOrFindAppHostProjectFileAsync(appHostFile, createSettingsFile: true, CancellationToken.None);

        Assert.Equal(appHostFile.FullName, result!.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAsync_RejectsInvalidSingleFileAppHost()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create apphost.cs without directive
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(appHostFile.FullName, @"using Aspire.Hosting;
var builder = DistributedApplication.CreateBuilder(args);
builder.Build().Run();");

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () =>
        {
            await projectLocator.UseOrFindAppHostProjectFileAsync(appHostFile, createSettingsFile: true, CancellationToken.None);
        });

        Assert.Equal(ErrorStrings.ProjectFileDoesntExist, ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAsync_AllowsSingleFileAppHostWithSiblingCsproj()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        // Add sibling .csproj file
        var csprojFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "SomeProject.csproj"));
        await File.WriteAllTextAsync(csprojFile.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext);

        // Allow the single-file apphost to be used explicitly even with sibling .csproj
        await projectLocator.UseOrFindAppHostProjectFileAsync(appHostFile, createSettingsFile: true, CancellationToken.None);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAsync_RejectsInvalidFileExtension()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var txtFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "readme.txt"));
        await File.WriteAllTextAsync(txtFile.FullName, "Some text file");

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () =>
        {
            await projectLocator.UseOrFindAppHostProjectFileAsync(txtFile, createSettingsFile: true, CancellationToken.None);
        });

        Assert.Equal(ErrorStrings.ProjectFileDoesntExist, ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAsync_ThrowsMultipleProjectsWhenBothCsprojAndSingleFileFound()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a valid .csproj AppHost
        var csprojFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(csprojFile.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        // Create a valid single-file AppHost in subdirectory (no sibling .csproj)
        var subDir = workspace.WorkspaceRoot.CreateSubdirectory("SingleFile");
        var appHostFile = new FileInfo(Path.Combine(subDir.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = CreateProjectLocatorWithSingleFileEnabled(executionContext, projectFile =>
        {
            if (projectFile.FullName == csprojFile.FullName)
            {
                return new AppHostValidationResult(IsValid: true);
            }
            return new AppHostValidationResult(IsValid: true);
        });

        // This should trigger the multiple projects selection, the test service will select the first one
        var result = await projectLocator.UseOrFindAppHostProjectFileAsync(null, createSettingsFile: true, CancellationToken.None);

        // The test interaction service returns the first item
        Assert.NotNull(result);
        // Should be one of the two valid candidates
        Assert.True(result.FullName == csprojFile.FullName || result.FullName == appHostFile.FullName);
    }

    private sealed class CliSettings
    {
        [JsonPropertyName("appHostPath")]
        public string? AppHostPath { get; set; }
    }

    private sealed class TestConfigurationService : IConfigurationService
    {
        public Task SetConfigurationAsync(string key, string value, bool isGlobal = false, CancellationToken cancellationToken = default)
        {
            // For test purposes, just return a completed task
            return Task.CompletedTask;
        }

        public Task<bool> DeleteConfigurationAsync(string key, bool isGlobal = false, CancellationToken cancellationToken = default)
        {
            // For test purposes, just return false (not found)
            return Task.FromResult(false);
        }

        public Task<Dictionary<string, string>> GetAllConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Dictionary<string, string>());
        }

        public Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default)
        {
            // For test purposes, just return null (not found)
            return Task.FromResult<string?>(null);
        }

        public string GetSettingsFilePath(bool isGlobal)
        {
            return string.Empty;
        }
    }

    public class TestFeatures : IFeatures
    {
        private readonly Dictionary<string, bool> _features = new();

        public TestFeatures SetFeature(string featureName, bool value)
        {
            _features[featureName] = value;
            return this;
        }

        public bool IsFeatureEnabled(string featureName, bool defaultValue = false)
        {
            return _features.TryGetValue(featureName, out var value) ? value : defaultValue;
        }
    }

    private static ProjectLocator CreateProjectLocatorWithSingleFileEnabled(CliExecutionContext executionContext, Func<FileInfo, AppHostValidationResult>? validateCallback = null)
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var projectFactory = new TestAppHostProjectFactory();
        if (validateCallback is not null)
        {
            projectFactory.ValidateAppHostCallback = validateCallback;
        }
        return new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAcceptsDirectoryPathWithSingleProject()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a subdirectory with a single project file
        var projectDirectory = workspace.WorkspaceRoot.CreateSubdirectory("MyAppHost");
        var projectFile = new FileInfo(Path.Combine(projectDirectory.FullName, "MyAppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var projectFactory = new TestAppHostProjectFactory
        {
            ValidateAppHostCallback = file =>
            {
                if (file.FullName == projectFile.FullName)
                {
                    return new AppHostValidationResult(IsValid: true);
                }
                return new AppHostValidationResult(IsValid: false);
            }
        };

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());

        // Pass directory as FileInfo (this is how System.CommandLine would parse it)
        var directoryAsFileInfo = new FileInfo(projectDirectory.FullName);
        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(directoryAsFileInfo, createSettingsFile: true);

        Assert.Equal(projectFile.FullName, returnedProjectFile!.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsWhenDirectoryHasNoProjects()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create an empty subdirectory
        var projectDirectory = workspace.WorkspaceRoot.CreateSubdirectory("EmptyDir");

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        // Pass directory as FileInfo
        var directoryAsFileInfo = new FileInfo(projectDirectory.FullName);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () =>
        {
            await projectLocator.UseOrFindAppHostProjectFileAsync(directoryAsFileInfo, createSettingsFile: true);
        });

        Assert.Equal(ErrorStrings.ProjectFileDoesntExist, ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFilePromptsWhenDirectoryHasMultipleProjects()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a subdirectory with multiple project files
        var projectDirectory = workspace.WorkspaceRoot.CreateSubdirectory("MultiProject");
        var projectFile1 = new FileInfo(Path.Combine(projectDirectory.FullName, "Project1.csproj"));
        await File.WriteAllTextAsync(projectFile1.FullName, "Not a real project file.");
        var projectFile2 = new FileInfo(Path.Combine(projectDirectory.FullName, "Project2.csproj"));
        await File.WriteAllTextAsync(projectFile2.FullName, "Not a real project file.");

        var projectFactory = new TestAppHostProjectFactory
        {
            ValidateAppHostCallback = file =>
            {
                // Both projects are AppHost projects
                if (file.FullName == projectFile1.FullName || file.FullName == projectFile2.FullName)
                {
                    return new AppHostValidationResult(IsValid: true);
                }
                return new AppHostValidationResult(IsValid: false);
            }
        };

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());

        // Pass directory as FileInfo
        var directoryAsFileInfo = new FileInfo(projectDirectory.FullName);

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(directoryAsFileInfo, createSettingsFile: true);

        // Should return the first project file (TestConsoleInteractionService returns the first choice)
        Assert.Equal(projectFile1.FullName, returnedProjectFile!.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAcceptsDirectoryPathWithSingleFileAppHost()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a subdirectory with a single-file apphost (no .csproj)
        var projectDirectory = workspace.WorkspaceRoot.CreateSubdirectory("MyAppHost");
        var appHostFile = new FileInfo(Path.Combine(projectDirectory.FullName, "apphost.cs"));
        await File.WriteAllTextAsync(
            appHostFile.FullName,
            """
            #:sdk Aspire.AppHost.Sdk
            using Aspire.Hosting;
            var builder = DistributedApplication.CreateBuilder(args);
            builder.Build().Run();
            """);

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, new TestAppHostProjectFactory(), new AspireCliTelemetry());

        // Pass directory as FileInfo (this is how System.CommandLine would parse it)
        var directoryAsFileInfo = new FileInfo(projectDirectory.FullName);
        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(directoryAsFileInfo, createSettingsFile: true);

        Assert.Equal(appHostFile.FullName, returnedProjectFile!.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileAcceptsDirectoryPathWithRecursiveSearch()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a directory structure with a project in a subdirectory
        var topDirectory = workspace.WorkspaceRoot.CreateSubdirectory("playground");
        var subDirectory = topDirectory.CreateSubdirectory("mongo");
        var projectFile = new FileInfo(Path.Combine(subDirectory.FullName, "Mongo.AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var projectFactory = new TestAppHostProjectFactory
        {
            ValidateAppHostCallback = file =>
            {
                if (file.FullName == projectFile.FullName)
                {
                    return new AppHostValidationResult(IsValid: true);
                }
                return new AppHostValidationResult(IsValid: false);
            }
        };

        var interactionService = new TestConsoleInteractionService();
        var configurationService = new TestConfigurationService();
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var projectLocator = new ProjectLocator(logger, executionContext, interactionService, configurationService, projectFactory, new AspireCliTelemetry());

        // Pass top directory as FileInfo - should find project in subdirectory
        var directoryAsFileInfo = new FileInfo(topDirectory.FullName);
        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(directoryAsFileInfo, createSettingsFile: true);

        Assert.Equal(projectFile.FullName, returnedProjectFile!.FullName);
    }
}

