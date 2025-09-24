// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class UpdateCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task UpdateCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WhenProjectOptionSpecified_PassesProjectFileToProjectLocator()
    {
        // Arrange
        FileInfo? capturedProjectFile = null;
        var specifiedProjectFile = new FileInfo("/test/path/MyApp.csproj");

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, cancellationToken) =>
                {
                    capturedProjectFile = projectFile;
                    return Task.FromResult(projectFile);
                }
            };

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse($"update --project {specifiedProjectFile.FullName}");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotNull(capturedProjectFile);
        Assert.Equal(specifiedProjectFile.FullName, capturedProjectFile.FullName);
    }

    [Fact]
    public async Task UpdateCommand_WhenProjectOptionNotSpecified_PassesNullToProjectLocator()
    {
        // Arrange
        FileInfo? capturedProjectFile = null;
        var returnedProjectFile = new FileInfo("/fake/AppHost.csproj");

        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, cancellationToken) =>
                {
                    capturedProjectFile = projectFile;
                    return Task.FromResult<FileInfo?>(returnedProjectFile);
                }
            };

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Null(capturedProjectFile);
    }

    [Fact]
    public async Task UpdateCommand_WhenNoProjectFileFound_ReturnsNonZeroExitCode()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, cancellationToken) =>
                {
                    return Task.FromResult<FileInfo?>(null);
                }
            };
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }
}

// Test implementation of IProjectUpdater
internal sealed class TestProjectUpdater : IProjectUpdater
{
    public Func<FileInfo, PackageChannel, CancellationToken, Task<ProjectUpdateResult>>? UpdateProjectAsyncCallback { get; set; }

    public Task<ProjectUpdateResult> UpdateProjectAsync(FileInfo projectFile, PackageChannel channel, CancellationToken cancellationToken = default)
    {
        if (UpdateProjectAsyncCallback != null)
        {
            return UpdateProjectAsyncCallback(projectFile, channel, cancellationToken);
        }

        // Default behavior
        return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
    }
}

// Test implementation of IPackagingService
internal sealed class TestPackagingService : IPackagingService
{
    public Func<CancellationToken, Task<IEnumerable<PackageChannel>>>? GetChannelsAsyncCallback { get; set; }

    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        if (GetChannelsAsyncCallback != null)
        {
            return GetChannelsAsyncCallback(cancellationToken);
        }

        // Default behavior - return a fake channel
        var testChannel = new PackageChannel("test", PackageChannelQuality.Stable, null, null!);
        return Task.FromResult<IEnumerable<PackageChannel>>(new[] { testChannel });
    }
}