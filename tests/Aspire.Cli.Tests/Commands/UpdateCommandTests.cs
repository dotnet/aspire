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
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, _, _) =>
                {
                    Assert.NotNull(projectFile);
                    return Task.FromResult<FileInfo?>(projectFile);
                }
            };

            options.InteractionServiceFactory = _ => new TestConsoleInteractionService();

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

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
        var result = command.Parse($"update --project AppHost.csproj");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void CleanupOldBackupFiles_DeletesFilesMatchingPattern()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var targetExePath = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe");
        var oldBackup1 = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe.old.1234567890");
        var oldBackup2 = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe.old.9876543210");
        var otherFile = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe.something");

        // Create test files
        File.WriteAllText(oldBackup1, "test");
        File.WriteAllText(oldBackup2, "test");
        File.WriteAllText(otherFile, "test");

        var updateCommand = CreateUpdateCommand(workspace);

        // Act
        updateCommand.CleanupOldBackupFiles(targetExePath);

        // Assert
        Assert.False(File.Exists(oldBackup1), "Old backup file should be deleted");
        Assert.False(File.Exists(oldBackup2), "Old backup file should be deleted");
        Assert.True(File.Exists(otherFile), "Other files should not be deleted");
    }

    [Fact]
    public void CleanupOldBackupFiles_HandlesInUseFilesGracefully()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var targetExePath = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe");
        var oldBackup = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe.old.1234567890");

        // Create and lock the backup file
        File.WriteAllText(oldBackup, "test");
        using var fileStream = new FileStream(oldBackup, FileMode.Open, FileAccess.Read, FileShare.None);

        var updateCommand = CreateUpdateCommand(workspace);

        // Act & Assert - should not throw exception
        updateCommand.CleanupOldBackupFiles(targetExePath);

        // File should still exist because it was locked
        Assert.True(File.Exists(oldBackup), "Locked file should still exist");
    }

    [Fact]
    public void CleanupOldBackupFiles_HandlesNonExistentDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var nonExistentPath = Path.Combine("C:", "NonExistent", "aspire.exe");
        var updateCommand = CreateUpdateCommand(workspace);

        // Act & Assert - should not throw exception
        updateCommand.CleanupOldBackupFiles(nonExistentPath);
    }

    [Fact]
    public void CleanupOldBackupFiles_HandlesEmptyDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var targetExePath = Path.Combine(workspace.WorkspaceRoot.FullName, "aspire.exe");
        var updateCommand = CreateUpdateCommand(workspace);

        // Act & Assert - should not throw exception
        updateCommand.CleanupOldBackupFiles(targetExePath);
    }

    private UpdateCommand CreateUpdateCommand(TemporaryWorkspace workspace)
    {
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<UpdateCommand>();
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
