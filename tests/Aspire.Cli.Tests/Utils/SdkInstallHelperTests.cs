// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Utils;

public class SdkInstallHelperTests
{
    [Fact]
    public async Task EnsureSdkInstalledAsync_ReturnsTrue_WhenSdkIsAvailable()
    {
        // Arrange
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (true, "10.0.100", "10.0.100", false)
        };
        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures();

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_ReturnsFalse_WhenSdkNotAvailableAndFeatureDisabled()
    {
        // Arrange
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100", false)
        };
        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, false);

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_InstallsWithoutPrompt_WhenNonInteractiveSdkInstallEnabled()
    {
        // Arrange
        var installed = false;
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100", false),
            InstallAsyncCallback = _ =>
            {
                installed = true;
                return Task.CompletedTask;
            }
        };
        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true)
            .SetFeature(KnownFeatures.NonInteractiveSdkInstall, true);

        // Act - non-interactive mode with feature enabled
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            hostEnvironment: null); // No host environment - simulating CI

        // Assert
        Assert.True(result);
        Assert.True(installed);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_DoesNotInstall_WhenNonInteractiveSdkInstallDisabledAndNoInteractiveInput()
    {
        // Arrange
        var installed = false;
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100", false),
            InstallAsyncCallback = _ =>
            {
                installed = true;
                return Task.CompletedTask;
            }
        };
        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true)
            .SetFeature(KnownFeatures.NonInteractiveSdkInstall, false);

        // Create a non-interactive host environment (like CI)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CI"] = "true"
            })
            .Build();
        var hostEnvironment = new CliHostEnvironment(configuration, nonInteractive: false);

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            hostEnvironment);

        // Assert - Should not install because we can't prompt and non-interactive SDK install is disabled
        Assert.False(result);
        Assert.False(installed);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_PromptsUser_WhenInteractiveInputSupported()
    {
        // Arrange
        var confirmCalled = false;
        var installed = false;
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100", false),
            InstallAsyncCallback = _ =>
            {
                installed = true;
                return Task.CompletedTask;
            }
        };
        var interactionService = new TestConsoleInteractionService
        {
            ConfirmCallback = (prompt, defaultValue) =>
            {
                confirmCalled = true;
                return true; // User confirms installation
            }
        };
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true)
            .SetFeature(KnownFeatures.NonInteractiveSdkInstall, false);

        // Create an interactive host environment
        var configuration = new ConfigurationBuilder().Build();
        var hostEnvironment = new CliHostEnvironment(configuration, nonInteractive: false);

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            hostEnvironment);

        // Assert - Should prompt and then install
        Assert.True(result);
        Assert.True(confirmCalled);
        Assert.True(installed);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_DoesNotInstall_WhenUserDeclinesPrompt()
    {
        // Arrange
        var installed = false;
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100", false),
            InstallAsyncCallback = _ =>
            {
                installed = true;
                return Task.CompletedTask;
            }
        };
        var interactionService = new TestConsoleInteractionService
        {
            ConfirmCallback = (prompt, defaultValue) => false // User declines installation
        };
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true)
            .SetFeature(KnownFeatures.NonInteractiveSdkInstall, false);

        // Create an interactive host environment
        var configuration = new ConfigurationBuilder().Build();
        var hostEnvironment = new CliHostEnvironment(configuration, nonInteractive: false);

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            hostEnvironment);

        // Assert - Should not install because user declined
        Assert.False(result);
        Assert.False(installed);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_InstallsWithForceInstall_SkipsPrompt()
    {
        // Arrange
        var installed = false;
        var confirmCalled = false;
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (true, "10.0.100", "10.0.100", true), // ForceInstall = true
            InstallAsyncCallback = _ =>
            {
                installed = true;
                return Task.CompletedTask;
            }
        };
        var interactionService = new TestConsoleInteractionService
        {
            ConfirmCallback = (prompt, defaultValue) =>
            {
                confirmCalled = true;
                return true;
            }
        };
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true);

        // Act
        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            hostEnvironment: null);

        // Assert - Should install without prompting because forceInstall is true
        Assert.True(result);
        Assert.True(installed);
        Assert.False(confirmCalled); // Prompt should be skipped
    }
}
