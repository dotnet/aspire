// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Aspire.Cli.Commands;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
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

        // On Windows, locked files cannot be deleted, so the file should still exist
        // On Mac/Linux, locked files can be deleted, so the file may be deleted
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.True(File.Exists(oldBackup), "Locked file should still exist on Windows");
        }
        else
        {
            Assert.False(File.Exists(oldBackup), "Locked file should be deleted on Mac/Linux");
        }
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

    [Fact]
    public async Task UpdateCommand_WhenNoProjectFound_PromptsForCliSelfUpdate()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var confirmCallbackInvoked = false;
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, _, _) =>
                {
                    // Simulate no project found by throwing ProjectLocatorException
                    throw new ProjectLocatorException(ErrorStrings.NoProjectFileFound);
                }
            };

            options.InteractionServiceFactory = _ => new TestConsoleInteractionService()
            {
                ConfirmCallback = (prompt, defaultValue) =>
                {
                    // Verify the correct prompt is shown
                    confirmCallbackInvoked = true;
                    Assert.Contains("Would you like to update the Aspire CLI", prompt);
                    return false; // User says no
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.True(confirmCallbackInvoked, "Confirm prompt should have been shown");
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WhenProjectUpdatedSuccessfully_PromptsForCliUpdate()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var confirmCallbackInvoked = false;
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, _, _) =>
                {
                    return Task.FromResult<FileInfo?>(new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj")));
                }
            };

            options.InteractionServiceFactory = _ => new TestConsoleInteractionService()
            {
                ConfirmCallback = (prompt, defaultValue) =>
                {
                    confirmCallbackInvoked = true;
                    // Verify the correct prompt is shown after project update
                    Assert.Contains("An update is available for the Aspire CLI", prompt);
                    return false; // User says no
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = true });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            // Configure update notifier to report that an update is available
            options.CliUpdateNotifierFactory = _ => new TestCliUpdateNotifier()
            {
                IsUpdateAvailableCallback = () => true
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --project AppHost.csproj");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.True(confirmCallbackInvoked, "Confirm prompt should have been shown after successful project update");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_SelfUpdate_WithQualityOption_DoesNotPromptForQuality()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var promptForSelectionInvoked = false;
        string? capturedQuality = null;
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService()
            {
                PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
                {
                    promptForSelectionInvoked = true;
                    // If this is called, it means the quality prompt was shown
                    Assert.Fail("Quality prompt should not be shown when --quality option is provided");
                    return "stable";
                }
            };

            options.CliDownloaderFactory = _ => new TestCliDownloader(workspace.WorkspaceRoot)
            {
                DownloadLatestCliAsyncCallback = (quality, ct) =>
                {
                    capturedQuality = quality;
                    // Create a fake archive file
                    var archivePath = Path.Combine(workspace.WorkspaceRoot.FullName, "test-cli.tar.gz");
                    File.WriteAllText(archivePath, "fake archive");
                    return Task.FromResult(archivePath);
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --self --quality daily");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.False(promptForSelectionInvoked, "Quality prompt should not be shown when --quality is provided");
        Assert.Equal("daily", capturedQuality);
    }
}

// Test implementation of ICliUpdateNotifier
internal sealed class TestCliUpdateNotifier : ICliUpdateNotifier
{
    public Func<bool>? IsUpdateAvailableCallback { get; set; }

    public Task CheckForCliUpdatesAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void NotifyIfUpdateAvailable()
    {
        // No-op for tests
    }

    public bool IsUpdateAvailable()
    {
        return IsUpdateAvailableCallback?.Invoke() ?? false;
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
