// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.DevTunnels.Tests;

public class DevTunnelCliInstallationManagerTests
{
    [Theory]
    [InlineData("1.0.1435", "1.0.1435", true)]
    [InlineData("1.0.1435", "1.0.1436", true)]
    [InlineData("1.0.1435", "1.1.1234", true)]
    [InlineData("1.0.1435", "2.0.0", true)]
    [InlineData("1.0.1435", "10.0.0", true)]
    [InlineData("1.9.0", "1.10.0", true)]
    [InlineData("1.0.1435", "1.0.1434", false)]
    [InlineData("1.0.1435", "1.0.0", false)]
    [InlineData("1.0.1435", "0.0.1", false)]
    [InlineData("1.2.0", "1.1.999", false)]
    public async Task OnResolvedAsync_ReturnsInvalidForUnsupportedVersion(string minVersion, string testVersion, bool expectedIsValid)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<DevTunnelCliInstallationManager>();
        var configuration = new ConfigurationBuilder().Build();
        var testCliVersion = Version.Parse(testVersion);
        var devTunnelClient = new TestDevTunnelClient(testCliVersion);

        var manager = new DevTunnelCliInstallationManager(devTunnelClient, configuration, new TestInteractionService(), logger, Version.Parse(minVersion));

        var (isValid, validationMessage) = await manager.OnResolvedAsync("thepath", CancellationToken.None);

        Assert.Equal(expectedIsValid, isValid);
        if (expectedIsValid)
        {
            Assert.Null(validationMessage);
        }
        else
        {
            Assert.Contains(minVersion, validationMessage);
            Assert.Contains(testVersion, validationMessage);
        }
    }

    [Fact]
    public void SuppressInstaller_DefaultsToFalse()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<DevTunnelCliInstallationManager>();
        var configuration = new ConfigurationBuilder().Build();
        var devTunnelClient = new TestDevTunnelClient(new Version(1, 0, 1435));

        var manager = new DevTunnelCliInstallationManager(devTunnelClient, configuration, new TestInteractionService(), logger);

        Assert.False(manager.SuppressInstaller);
    }

    [Fact]
    public void SuppressInstaller_CanBeSet()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<DevTunnelCliInstallationManager>();
        var configuration = new ConfigurationBuilder().Build();
        var devTunnelClient = new TestDevTunnelClient(new Version(1, 0, 1435));

        var manager = new DevTunnelCliInstallationManager(devTunnelClient, configuration, new TestInteractionService(), logger);
        manager.SuppressInstaller = true;

        Assert.True(manager.SuppressInstaller);
    }

    [Fact]
    public async Task OnResolvedAsync_WithUnsupportedVersion_WhenSuppressed_ReturnsInvalid()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<DevTunnelCliInstallationManager>();
        var configuration = new ConfigurationBuilder().Build();
        var testCliVersion = new Version(1, 0, 0); // Below minimum
        var devTunnelClient = new TestDevTunnelClient(testCliVersion);
        var interactionService = new TestInteractionService();

        var manager = new DevTunnelCliInstallationManager(devTunnelClient, configuration, interactionService, logger, new Version(1, 0, 1435));
        manager.SuppressInstaller = true;

        var (isValid, validationMessage) = await manager.OnResolvedAsync("thepath", CancellationToken.None);

        Assert.False(isValid);
        Assert.NotNull(validationMessage);
        // Should not have called confirmation prompt since installer is suppressed
        Assert.False(interactionService.ConfirmationPromptCalled);
    }

    [Fact]
    public async Task OnResolvedAsync_WithUnsupportedVersion_WhenInteractionNotAvailable_ReturnsInvalid()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<DevTunnelCliInstallationManager>();
        var configuration = new ConfigurationBuilder().Build();
        var testCliVersion = new Version(1, 0, 0); // Below minimum
        var devTunnelClient = new TestDevTunnelClient(testCliVersion);
        var interactionService = new TestInteractionService { IsAvailable = false };

        var manager = new DevTunnelCliInstallationManager(devTunnelClient, configuration, interactionService, logger, new Version(1, 0, 1435));

        var (isValid, validationMessage) = await manager.OnResolvedAsync("thepath", CancellationToken.None);

        Assert.False(isValid);
        Assert.NotNull(validationMessage);
        Assert.False(interactionService.ConfirmationPromptCalled);
    }

    [Fact]
    public void DevTunnelCliInstaller_GetInstallCommand_ReturnsNonEmptyString()
    {
        var command = DevTunnelCliInstaller.GetInstallCommand();

        // Should return a command for the current platform
        Assert.False(string.IsNullOrEmpty(command));
    }

    [Fact]
    public void DevTunnelCliInstaller_GetPackageManagerName_ReturnsNonEmptyString()
    {
        var name = DevTunnelCliInstaller.GetPackageManagerName();

        Assert.False(string.IsNullOrEmpty(name));
    }

    [Fact]
    public void DevTunnelCliInstaller_IsInstallSupported_ReturnsTrue()
    {
        // Should be supported on Windows, macOS, and Linux
        Assert.True(DevTunnelCliInstaller.IsInstallSupported);
    }

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private sealed class TestInteractionService : IInteractionService
    {
        public bool IsAvailable { get; set; } = true;
        public bool ConfirmationPromptCalled { get; private set; }
        public bool NotificationPromptCalled { get; private set; }
        public bool ConfirmationResult { get; set; } = false;
        public string? LastConfirmationTitle { get; private set; }
        public string? LastConfirmationMessage { get; private set; }
        public string? LastNotificationTitle { get; private set; }
        public string? LastNotificationMessage { get; private set; }

        public Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            ConfirmationPromptCalled = true;
            LastConfirmationTitle = title;
            LastConfirmationMessage = message;
            return Task.FromResult(InteractionResult.Ok(ConfirmationResult));
        }

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<InteractionInputCollection>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<bool>> PromptNotificationAsync(string title, string message, NotificationInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            NotificationPromptCalled = true;
            LastNotificationTitle = title;
            LastNotificationMessage = message;
            return Task.FromResult(InteractionResult.Ok(true));
        }
    }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    private sealed class TestDevTunnelClient(Version cliVersion) : IDevTunnelClient
    {
        public Task<Version> GetVersionAsync(ILogger? logger = null, CancellationToken cancellationToken = default) => Task.FromResult(cliVersion);

        public Task<DevTunnelPortStatus> CreatePortAsync(string tunnelId, int portNumber, DevTunnelPortOptions options, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DevTunnelStatus> CreateTunnelAsync(string tunnelId, DevTunnelOptions options, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DevTunnelAccessStatus> GetAccessAsync(string tunnelId, int? portNumber = null, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DevTunnelStatus> GetTunnelAsync(string tunnelId, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UserLoginStatus> GetUserLoginStatusAsync(ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UserLoginStatus> UserLoginAsync(LoginProvider provider, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DevTunnelPortList> GetPortListAsync(string tunnelId, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DevTunnelPortDeleteResult> DeletePortAsync(string tunnelId, int portNumber, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
