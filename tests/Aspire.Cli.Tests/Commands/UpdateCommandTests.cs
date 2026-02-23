// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Microsoft.AspNetCore.InternalTesting;

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

        var exitCode = await result.InvokeAsync().DefaultTimeout();
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

        var exitCode = await result.InvokeAsync().DefaultTimeout();

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

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.True(confirmCallbackInvoked, "Confirm prompt should have been shown");
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WhenProjectUpdatedSuccessfully_AndChannelSupportsCliDownload_PromptsForCliUpdate()
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

            // Return a channel with CliDownloadBaseUrl to enable CLI update prompts
            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (cancellationToken) =>
                {
                    var stableChannel = PackageChannel.CreateExplicitChannel(
                        "stable",
                        PackageChannelQuality.Stable,
                        new[] { new PackageMapping("Aspire*", "https://api.nuget.org/v3/index.json") },
                        null!,
                        configureGlobalPackagesFolder: false,
                        cliDownloadBaseUrl: "https://aka.ms/dotnet/9/aspire/ga/daily");
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { stableChannel });
                }
            };

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

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.True(confirmCallbackInvoked, "Confirm prompt should have been shown after successful project update");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WhenChannelHasNoCliDownloadUrl_DoesNotPromptForCliUpdate()
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

            // Return a channel without CliDownloadBaseUrl (like PR channels)
            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (cancellationToken) =>
                {
                    var prChannel = PackageChannel.CreateExplicitChannel(
                        "pr-12658",
                        PackageChannelQuality.Prerelease,
                        new[] { new PackageMapping("Aspire*", "/path/to/pr/hive") },
                        null!,
                        configureGlobalPackagesFolder: false,
                        cliDownloadBaseUrl: null); // No CLI download URL for PR channels
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { prChannel });
                }
            };

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

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.False(confirmCallbackInvoked, "Confirm prompt should NOT have been shown for channels without CLI download support");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_SelfUpdate_WithChannelOption_DoesNotPromptForChannel()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var promptForSelectionInvoked = false;
        string? capturedChannel = null;
        
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => new TestConsoleInteractionService()
            {
                PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
                {
                    promptForSelectionInvoked = true;
                    // If this is called, it means the channel prompt was shown
                    Assert.Fail("Channel prompt should not be shown when --channel option is provided");
                    return "stable";
                }
            };

            options.CliDownloaderFactory = _ => new TestCliDownloader(workspace.WorkspaceRoot)
            {
                DownloadLatestCliAsyncCallback = (channel, ct) =>
                {
                    capturedChannel = channel;
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
        var result = command.Parse("update --self --channel daily");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.False(promptForSelectionInvoked, "Channel prompt should not be shown when --channel is provided");
        Assert.Equal("daily", capturedChannel);
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

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.False(promptForSelectionInvoked, "Quality prompt should not be shown when --quality is provided");
        Assert.Equal("daily", capturedQuality);
    }

    [Fact]
    public async Task UpdateCommand_SelfUpdate_WithChannelOption_TracksChannelParameter()
    {
        // This test verifies that the channel parameter flows through the self-update command.
        // Full integration testing of SetConfigurationAsync would require creating a valid
        // tar.gz archive with a working CLI executable, which is complex for a unit test.
        // The test verifies the channel value is properly captured and would be passed
        // to configuration service if the extraction succeeds.
        
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        string? capturedChannel = null;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliDownloaderFactory = _ => new TestCliDownloader(workspace.WorkspaceRoot)
            {
                DownloadLatestCliAsyncCallback = (channel, ct) =>
                {
                    capturedChannel = channel;
                    // Create a fake archive file - extraction will fail but channel is captured
                    var archivePath = Path.Combine(workspace.WorkspaceRoot.FullName, "test-cli.tar.gz");
                    File.WriteAllText(archivePath, "fake archive");
                    return Task.FromResult(archivePath);
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --self --channel daily");

        // Note: exitCode will be non-zero because extraction fails, but that's okay for this test
        await result.InvokeAsync().DefaultTimeout();

        // Assert - verify the channel parameter was correctly passed through
        Assert.Equal("daily", capturedChannel);
    }

    [Fact]
    public async Task UpdateCommand_ProjectUpdate_WithChannelOption_DoesNotPromptForChannel()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var promptForSelectionInvoked = false;
        PackageChannel? capturedChannel = null;
        
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
                PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
                {
                    promptForSelectionInvoked = true;
                    // If this is called, it means the channel prompt was shown
                    Assert.Fail("Channel prompt should not be shown when --channel option is provided");
                    return choices.Cast<PackageChannel>().First();
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    capturedChannel = channel;
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = true });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (ct) =>
                {
                    // Create test channels matching the expected names
                    var stableChannel = new PackageChannel("stable", PackageChannelQuality.Stable, null, null!);
                    var dailyChannel = new PackageChannel("daily", PackageChannelQuality.Prerelease, null, null!);
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { stableChannel, dailyChannel });
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --channel daily");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.False(promptForSelectionInvoked, "Channel prompt should not be shown when --channel is provided");
        Assert.NotNull(capturedChannel);
        Assert.Equal("daily", capturedChannel.Name);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_ProjectUpdate_WithQualityOption_DoesNotPromptForChannel()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var promptForSelectionInvoked = false;
        PackageChannel? capturedChannel = null;
        
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
                PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
                {
                    promptForSelectionInvoked = true;
                    // If this is called, it means the channel prompt was shown
                    Assert.Fail("Channel prompt should not be shown when --quality option is provided");
                    return choices.Cast<PackageChannel>().First();
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    capturedChannel = channel;
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = true });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (ct) =>
                {
                    // Create test channels matching the expected names
                    var stableChannel = new PackageChannel("stable", PackageChannelQuality.Stable, null, null!);
                    var dailyChannel = new PackageChannel("daily", PackageChannelQuality.Prerelease, null, null!);
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { stableChannel, dailyChannel });
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --quality daily");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.False(promptForSelectionInvoked, "Channel prompt should not be shown when --quality is provided");
        Assert.NotNull(capturedChannel);
        Assert.Equal("daily", capturedChannel.Name);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_ProjectUpdate_WithInvalidQuality_DisplaysError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var errorDisplayed = false;
        string? errorMessage = null;
        
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
                DisplayErrorCallback = (message) =>
                {
                    errorDisplayed = true;
                    errorMessage = message;
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater();

            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (ct) =>
                {
                    // Create test channels matching the expected names
                    var stableChannel = new PackageChannel("stable", PackageChannelQuality.Stable, null, null!);
                    var dailyChannel = new PackageChannel("daily", PackageChannelQuality.Prerelease, null, null!);
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { stableChannel, dailyChannel });
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --quality invalid");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.True(errorDisplayed, "Error should be displayed for invalid quality");
        Assert.NotNull(errorMessage);
        Assert.Contains("invalid", errorMessage);
        Assert.Contains("stable", errorMessage);
        Assert.Contains("daily", errorMessage);
        Assert.Equal(ExitCodeConstants.FailedToUpgradeProject, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_ProjectUpdate_ChannelTakesPrecedenceOverQuality()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var promptForSelectionInvoked = false;
        PackageChannel? capturedChannel = null;
        
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
                PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
                {
                    promptForSelectionInvoked = true;
                    Assert.Fail("Channel prompt should not be shown when --channel option is provided");
                    return choices.Cast<PackageChannel>().First();
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    capturedChannel = channel;
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = true });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (ct) =>
                {
                    var stableChannel = new PackageChannel("stable", PackageChannelQuality.Stable, null, null!);
                    var dailyChannel = new PackageChannel("daily", PackageChannelQuality.Prerelease, null, null!);
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { stableChannel, dailyChannel });
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act - specify both --channel and --quality, --channel should win
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --channel stable --quality daily");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert - should use "stable" from --channel, not "daily" from --quality
        Assert.False(promptForSelectionInvoked, "Channel prompt should not be shown");
        Assert.NotNull(capturedChannel);
        Assert.Equal("stable", capturedChannel.Name);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_ProjectUpdate_WhenCancelled_DisplaysCancellationMessage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a hive directory so the channel prompt is shown
        var hivesDir = workspace.CreateDirectory(".aspire").CreateSubdirectory("hives");
        hivesDir.CreateSubdirectory("pr-12345");

        var cancellationMessageDisplayed = false;
        
        var wrappedService = new CancellationTrackingInteractionService(new TestConsoleInteractionService()
        {
            PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
            {
                // Simulate user pressing Ctrl+C during selection prompt
                throw new OperationCanceledException();
            }
        });
        wrappedService.OnCancellationMessageDisplayed = () => cancellationMessageDisplayed = true;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = _ => new TestProjectLocator()
            {
                UseOrFindAppHostProjectFileAsyncCallback = (projectFile, _, _) =>
                {
                    return Task.FromResult<FileInfo?>(new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj")));
                }
            };

            options.InteractionServiceFactory = _ => wrappedService;

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (ct) =>
                {
                    var stableChannel = new PackageChannel("stable", PackageChannelQuality.Stable, null, null!);
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { stableChannel });
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.True(cancellationMessageDisplayed, "Cancellation message should have been displayed");
        Assert.Equal(ExitCodeConstants.FailedToUpgradeProject, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_WithoutHives_UsesImplicitChannelWithoutPrompting()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var promptForSelectionInvoked = false;
        var updatedWithChannel = string.Empty;

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
                PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
                {
                    promptForSelectionInvoked = true;
                    return choices.Cast<object>().First();
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    updatedWithChannel = channel.Name;
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService()
            {
                GetChannelsAsyncCallback = (ct) =>
                {
                    var fakeCache = new FakeNuGetPackageCache();
                    var implicitChannel = PackageChannel.CreateImplicitChannel(fakeCache);
                    return Task.FromResult<IEnumerable<PackageChannel>>(new[] { implicitChannel });
                }
            };
        });

        var provider = services.BuildServiceProvider();

        // Act - without hives, should automatically use implicit channel
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(promptForSelectionInvoked, "Channel selection prompt should not be shown when there are no hives");
        Assert.Equal("default", updatedWithChannel); // Implicit channel is named "default"
    }

    [Fact]
    public async Task UpdateCommand_SelfUpdate_WhenCancelled_DisplaysCancellationMessage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a hive directory so the channel prompt is shown
        var hivesDir = workspace.CreateDirectory(".aspire").CreateSubdirectory("hives");
        hivesDir.CreateSubdirectory("pr-12345");

        var cancellationMessageDisplayed = false;
        
        var wrappedService = new CancellationTrackingInteractionService(new TestConsoleInteractionService()
        {
            PromptForSelectionCallback = (prompt, choices, formatter, ct) =>
            {
                // Simulate user pressing Ctrl+C during channel selection prompt
                throw new OperationCanceledException();
            }
        });
        wrappedService.OnCancellationMessageDisplayed = () => cancellationMessageDisplayed = true;

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.InteractionServiceFactory = _ => wrappedService;

            options.CliDownloaderFactory = _ => new TestCliDownloader(workspace.WorkspaceRoot);
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update --self");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.True(cancellationMessageDisplayed, "Cancellation message should have been displayed");
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task UpdateCommand_SelfOption_IsAvailableAndParseable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.CliDownloaderFactory = _ => new TestCliDownloader(workspace.WorkspaceRoot)
            {
                DownloadLatestCliAsyncCallback = (channel, ct) =>
                {
                    // Create a fake archive file
                    var archivePath = Path.Combine(workspace.WorkspaceRoot.FullName, "test-cli.tar.gz");
                    File.WriteAllText(archivePath, "fake archive");
                    return Task.FromResult(archivePath);
                }
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();
        
        // Act - Parse command with --self option
        var result = command.Parse("update --self --channel stable");
        
        // Assert - Command should parse successfully without errors
        Assert.Empty(result.Errors);
    }
}

// Helper class to track DisplayCancellationMessage calls
internal sealed class CancellationTrackingInteractionService : IInteractionService
{
    private readonly IInteractionService _innerService;

    public Action? OnCancellationMessageDisplayed { get; set; }

    public CancellationTrackingInteractionService(IInteractionService innerService)
    {
        _innerService = innerService;
    }

    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => _innerService.ShowStatusAsync(statusText, action);
    public void ShowStatus(string statusText, Action action) => _innerService.ShowStatus(statusText, action);
    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default) 
        => _innerService.PromptForStringAsync(promptText, defaultValue, validator, isSecret, required, cancellationToken);
    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default) 
        => _innerService.ConfirmAsync(promptText, defaultValue, cancellationToken);
    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull 
        => _innerService.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
    public Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull 
        => _innerService.PromptForSelectionsAsync(promptText, choices, choiceFormatter, cancellationToken);
    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion) 
        => _innerService.DisplayIncompatibleVersionError(ex, appHostHostingVersion);
    public void DisplayError(string errorMessage) => _innerService.DisplayError(errorMessage);
    public void DisplayMessage(string emoji, string message) => _innerService.DisplayMessage(emoji, message);
    public void DisplayPlainText(string text) => _innerService.DisplayPlainText(text);
    public void DisplayRawText(string text) => _innerService.DisplayRawText(text);
    public void DisplayMarkdown(string markdown) => _innerService.DisplayMarkdown(markdown);
    public void DisplayMarkupLine(string markup) => _innerService.DisplayMarkupLine(markup);
    public void DisplaySuccess(string message) => _innerService.DisplaySuccess(message);
    public void DisplaySubtleMessage(string message, bool escapeMarkup = true) => _innerService.DisplaySubtleMessage(message, escapeMarkup);
    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) => _innerService.DisplayLines(lines);
    public void DisplayCancellationMessage() 
    {
        OnCancellationMessageDisplayed?.Invoke();
        _innerService.DisplayCancellationMessage();
    }
    public void DisplayEmptyLine() => _innerService.DisplayEmptyLine();
    public void DisplayVersionUpdateNotification(string newerVersion, string? updateCommand = null) 
        => _innerService.DisplayVersionUpdateNotification(newerVersion, updateCommand);
    public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) 
        => _innerService.WriteConsoleLog(message, lineNumber, type, isErrorMessage);
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

public class UpdateCommandSkillFileTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task UpdateCommand_DetectsOutdatedSkillFile_PromptsForUpdate()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create an outdated skill file
        var skillFileDir = workspace.CreateDirectory(Path.Combine(".github", "skills", "aspire"));
        var skillFilePath = Path.Combine(skillFileDir.FullName, "SKILL.md");
        File.WriteAllText(skillFilePath, "# Outdated Content\n\nThis is old skill file content.");

        var confirmCallbackInvoked = false;
        string? capturedPrompt = null;

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
                    capturedPrompt = prompt;
                    return false; // User says no to update
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            // Configure GitRepository to return the workspace root as git root
            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(workspace.WorkspaceRoot)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(confirmCallbackInvoked, "Should prompt for skill file update");
        Assert.NotNull(capturedPrompt);
        Assert.Contains("SKILL.md", capturedPrompt);
        Assert.Contains("out of date", capturedPrompt);
    }

    [Fact]
    public async Task UpdateCommand_WithOutdatedSkillFile_UpdatesWhenConfirmed()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create an outdated skill file
        var skillFileDir = workspace.CreateDirectory(Path.Combine(".github", "skills", "aspire"));
        var skillFilePath = Path.Combine(skillFileDir.FullName, "SKILL.md");
        File.WriteAllText(skillFilePath, "# Outdated Content\n\nThis is old skill file content.");

        var successMessageDisplayed = false;

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
                    // User confirms update
                    return true;
                },
                DisplaySuccessCallback = (message) =>
                {
                    if (message.Contains("skill file"))
                    {
                        successMessageDisplayed = true;
                    }
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(workspace.WorkspaceRoot)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(successMessageDisplayed, "Should display success message after update");
        
        // Verify file was updated with new content
        var updatedContent = File.ReadAllText(skillFilePath);
        Assert.Contains("# Aspire Skill", updatedContent);
        Assert.DoesNotContain("Outdated Content", updatedContent);
    }

    [Fact]
    public async Task UpdateCommand_WithUpToDateSkillFile_DoesNotPrompt()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a skill file with current content
        var skillFileDir = workspace.CreateDirectory(Path.Combine(".github", "skills", "aspire"));
        var skillFilePath = Path.Combine(skillFileDir.FullName, "SKILL.md");
        File.WriteAllText(skillFilePath, Aspire.Cli.Agents.CommonAgentApplicators.SkillFileContent);

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
                    if (prompt.Contains("SKILL.md"))
                    {
                        confirmCallbackInvoked = true;
                    }
                    return false;
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(workspace.WorkspaceRoot)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(confirmCallbackInvoked, "Should not prompt when skill file is up to date");
    }

    [Fact]
    public async Task UpdateCommand_WithNoSkillFile_DoesNotPrompt()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // No skill file exists

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
                    if (prompt.Contains("SKILL.md"))
                    {
                        confirmCallbackInvoked = true;
                    }
                    return false;
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(workspace.WorkspaceRoot)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(confirmCallbackInvoked, "Should not prompt when no skill file exists");
    }

    [Fact]
    public async Task UpdateCommand_WhenNotInGitRepo_DoesNotCheckSkillFile()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create an outdated skill file - but since we're not in a git repo, it shouldn't be detected
        var skillFileDir = workspace.CreateDirectory(Path.Combine(".github", "skills", "aspire"));
        var skillFilePath = Path.Combine(skillFileDir.FullName, "SKILL.md");
        File.WriteAllText(skillFilePath, "# Outdated Content");

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
                    if (prompt.Contains("SKILL.md"))
                    {
                        confirmCallbackInvoked = true;
                    }
                    return false;
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            // GitRepository returns null (not in a git repo)
            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(null)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(confirmCallbackInvoked, "Should not prompt when not in a git repo");
    }

    [Theory]
    [InlineData(".github/skills/aspire", "GitHub Copilot")]
    [InlineData(".opencode/skill/aspire", "OpenCode")]
    [InlineData(".claude/skills/aspire", "Claude Code")]
    public async Task UpdateCommand_DetectsOutdatedSkillFile_AllLocations(string skillFileRelativeDir, string agentName)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create an outdated skill file at the specified location
        var skillFileDir = workspace.CreateDirectory(skillFileRelativeDir);
        var skillFilePath = Path.Combine(skillFileDir.FullName, "SKILL.md");
        File.WriteAllText(skillFilePath, $"# Outdated {agentName} Content\n\nThis is old skill file content.");

        var confirmCallbackInvoked = false;
        string? capturedPrompt = null;

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
                    capturedPrompt = prompt;
                    return false; // User says no to update
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            // Configure GitRepository to return the workspace root as git root
            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(workspace.WorkspaceRoot)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(confirmCallbackInvoked, $"Should prompt for {agentName} skill file update");
        Assert.NotNull(capturedPrompt);
        Assert.Contains("SKILL.md", capturedPrompt);
        Assert.Contains("out of date", capturedPrompt);
    }

    [Fact]
    public async Task UpdateCommand_WithMultipleOutdatedSkillFiles_PromptsForEach()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create outdated skill files at all three locations
        var githubDir = workspace.CreateDirectory(Path.Combine(".github", "skills", "aspire"));
        var opencodeDir = workspace.CreateDirectory(Path.Combine(".opencode", "skill", "aspire"));
        var claudeDir = workspace.CreateDirectory(Path.Combine(".claude", "skills", "aspire"));
        
        File.WriteAllText(Path.Combine(githubDir.FullName, "SKILL.md"), "# Outdated GitHub");
        File.WriteAllText(Path.Combine(opencodeDir.FullName, "SKILL.md"), "# Outdated OpenCode");
        File.WriteAllText(Path.Combine(claudeDir.FullName, "SKILL.md"), "# Outdated Claude");

        var promptCount = 0;
        var capturedPrompts = new List<string>();

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
                    promptCount++;
                    capturedPrompts.Add(prompt);
                    return true; // User says yes to update all
                }
            };

            options.DotNetCliRunnerFactory = _ => new TestDotNetCliRunner();

            options.ProjectUpdaterFactory = _ => new TestProjectUpdater()
            {
                UpdateProjectAsyncCallback = (projectFile, channel, cancellationToken) =>
                {
                    return Task.FromResult(new ProjectUpdateResult { UpdatedApplied = false });
                }
            };

            options.PackagingServiceFactory = _ => new TestPackagingService();

            // Configure GitRepository to return the workspace root as git root
            options.GitRepositoryFactory = _ => new TestGitRepository()
            {
                GetRootAsyncCallback = (_) => Task.FromResult<DirectoryInfo?>(workspace.WorkspaceRoot)
            };
        });

        var provider = services.BuildServiceProvider();

        // Act
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("update");

        var exitCode = await result.InvokeAsync().DefaultTimeout();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal(3, promptCount);
        Assert.Contains(capturedPrompts, p => p.Contains(".github"));
        Assert.Contains(capturedPrompts, p => p.Contains(".opencode"));
        Assert.Contains(capturedPrompts, p => p.Contains(".claude"));

        // Verify all files were updated
        Assert.Equal(Aspire.Cli.Agents.CommonAgentApplicators.SkillFileContent, File.ReadAllText(Path.Combine(githubDir.FullName, "SKILL.md")));
        Assert.Equal(Aspire.Cli.Agents.CommonAgentApplicators.SkillFileContent, File.ReadAllText(Path.Combine(opencodeDir.FullName, "SKILL.md")));
        Assert.Equal(Aspire.Cli.Agents.CommonAgentApplicators.SkillFileContent, File.ReadAllText(Path.Combine(claudeDir.FullName, "SKILL.md")));
    }
}
