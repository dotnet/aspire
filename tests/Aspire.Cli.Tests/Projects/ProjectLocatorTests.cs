// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Cli.Tests.Projects;

public class ProjectLocatorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsIfExplicitProjectFileDoesNotExist()
    {
        var logger = NullLogger<ProjectLocator>.Instance;

        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));

        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();
        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () => {
            await projectLocator.UseOrFindAppHostProjectFileAsync(projectFile);
        });

        Assert.Equal("Project file does not exist.", ex.Message);
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

        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();
        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null);

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

        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();
        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null);

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

        // Verify both files were created
        Assert.True(projectFile1.Exists, $"Project file 1 should exist: {projectFile1.FullName}");
        Assert.True(projectFile2.Exists, $"Project file 2 should exist: {projectFile2.FullName}");
        
        // Verify string comparison works as expected
        var comparison = projectFile1.FullName.CompareTo(projectFile2.FullName);
        outputHelper.WriteLine($"File comparison result: {comparison}");
        outputHelper.WriteLine($"File 1: {projectFile1.FullName}");
        outputHelper.WriteLine($"File 2: {projectFile2.FullName}");
        
        Assert.True(comparison < 0, "AppHost1.csproj should come before AppHost2.csproj alphabetically");

        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();
        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var selectedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(null);

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

        var runner = new TestDotNetCliRunner();
        runner.GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) => {
            if (projectFile.FullName == appHostProject.FullName)
            {
                return (0, true, VersionHelper.GetDefaultTemplateVersion());
            }
            else
            {
                return (0, false, null);
            }
        };

        var interactionService = new TestInteractionService();

        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);
        var foundAppHost = await projectLocator.UseOrFindAppHostProjectFileAsync(null);
        Assert.Equal(appHostProject.FullName, foundAppHost?.FullName);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileThrowsIfNoProjectWasFound()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();

        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var ex = await Assert.ThrowsAsync<ProjectLocatorException>(async () =>{
            await projectLocator.UseOrFindAppHostProjectFileAsync(null);
        });

        Assert.Equal("No project file found.", ex.Message);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileReturnsExplicitProjectIfExistsAndProvided()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();
        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(projectFile);

        Assert.Equal(projectFile, returnedProjectFile);
    }

    [Fact]
    public async Task UseOrFindAppHostProjectFileReturnsProjectFileInDirectoryIfNotExplicitlyProvided()
    {
        var logger = NullLogger<ProjectLocator>.Instance;
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));
        await File.WriteAllTextAsync(projectFile.FullName, "Not a real project file.");

        var runner = new TestDotNetCliRunner();
        var interactionService = new TestInteractionService();

        var projectLocator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        var returnedProjectFile = await projectLocator.UseOrFindAppHostProjectFileAsync(null);
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

        var runner = new TestDotNetCliRunner();
        runner.GetAppHostInformationAsyncCallback = (_, _, _) =>
        {
            return (0, true, VersionHelper.GetDefaultTemplateVersion());
        };

        var interactionService = new TestInteractionService();
        var locator = new ProjectLocator(logger, runner, workspace.WorkspaceRoot, interactionService);

        await locator.UseOrFindAppHostProjectFileAsync(null, CancellationToken.None);

        var settingsFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "settings.json"));
        Assert.True(settingsFile.Exists, "Settings file should exist.");

        var settingsJson = await File.ReadAllTextAsync(settingsFile.FullName);
        var settings = JsonSerializer.Deserialize<CliSettings>(settingsJson);

        Assert.NotNull(settings);
        Assert.NotNull(settings!.AppHostPath);
        Assert.DoesNotContain('\\', settings.AppHostPath); // Ensure no backslashes
        Assert.Contains('/', settings.AppHostPath); // Ensure forward slashes
    }

    private sealed class CliSettings
    {
        [JsonPropertyName("appHostPath")]
        public string? AppHostPath { get; set; }
    }
}
