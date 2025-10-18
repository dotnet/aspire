// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Resources;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Aspire.Hosting.Tests.Dcp;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public sealed class DcpHostNotificationTests
{
    [Fact]
    public void DcpHost_WithIInteractionService_CanBeConstructed()
    {
        // Arrange
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions());
        var dependencyCheckService = new TestDcpDependencyCheckService();
        var interactionService = new TestInteractionService();
        var locations = new Locations();
        var applicationModel = new DistributedApplicationModel(new ResourceCollection());
        var timeProvider = new FakeTimeProvider();

        // Act & Assert - should not throw
        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        Assert.NotNull(dcpHost);
    }

    [Fact]
    public async Task DcpHost_WithUnhealthyContainerRuntime_ShowsNotification()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker", CliPath = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh" });
        var dependencyCheckService = new TestDcpDependencyCheckService
        {
            // Container installed but not running - unhealthy state
            DcpInfoResult = new DcpInfo
            {
                Containers = new DcpContainersInfo
                {
                    Installed = true,
                    Running = false,
                    Error = "Docker daemon is not running"
                }
            }
        };
        var interactionService = new TestInteractionService { IsAvailable = true };
        var locations = new Locations();
        var timeProvider = new FakeTimeProvider();

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        // Act
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None).DefaultTimeout();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);

        // Assert
        Assert.Equal(InteractionStrings.ContainerRuntimeUnhealthyTitle, interaction.Title);
        Assert.Contains("docker", interaction.Message);
        Assert.Contains(string.Format(CultureInfo.InvariantCulture, InteractionStrings.ContainerRuntimeUnhealthyMessage, "docker"), interaction.Message);
        var notificationOptions = Assert.IsType<NotificationInteractionOptions>(interaction.Options);
        Assert.Equal(MessageIntent.Error, notificationOptions.Intent);
    }

    [Fact]
    public async Task DcpHost_WithHealthyContainerRuntime_DoesNotShowNotification()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker", CliPath = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh" });
        var dependencyCheckService = new TestDcpDependencyCheckService
        {
            // Container installed and running - healthy state
            DcpInfoResult = new DcpInfo
            {
                Containers = new DcpContainersInfo
                {
                    Installed = true,
                    Running = true,
                    Error = null
                }
            }
        };
        var interactionService = new TestInteractionService { IsAvailable = true };
        var locations = new Locations();
        var timeProvider = new FakeTimeProvider();

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        // Act
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None).DefaultTimeout();

        // Use a short timeout to check that no notification is sent
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var hasInteraction = false;
        try
        {
            await interactionService.Interactions.Reader.ReadAsync(cts.Token);
            hasInteraction = true;
        }
        catch (OperationCanceledException)
        {
            // Expected - no notification should be sent
        }

        // Assert - no notification should be shown for healthy runtime
        Assert.False(hasInteraction);
    }

    [Fact]
    public async Task DcpHost_WithDashboardDisabled_DoesNotShowNotification()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker", CliPath = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh" });
        var dependencyCheckService = new TestDcpDependencyCheckService
        {
            // Container installed but not running - unhealthy state
            DcpInfoResult = new DcpInfo
            {
                Containers = new DcpContainersInfo
                {
                    Installed = true,
                    Running = false,
                    Error = "Docker daemon is not running"
                }
            }
        };
        var interactionService = new TestInteractionService { IsAvailable = false }; // Dashboard disabled
        var locations = new Locations();
        var timeProvider = new FakeTimeProvider();

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        // Act
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None).DefaultTimeout();

        // Use a short timeout to check that no notification is sent
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var hasInteraction = false;
        try
        {
            await interactionService.Interactions.Reader.ReadAsync(cts.Token);
            hasInteraction = true;
        }
        catch (OperationCanceledException)
        {
            // Expected - no notification should be sent when dashboard is disabled
        }

        // Assert - no notification should be shown when dashboard is disabled
        Assert.False(hasInteraction);
    }

    [Fact]
    public async Task DcpHost_WithPodmanUnhealthy_ShowsCorrectMessage()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "podman", CliPath = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh" });
        var dependencyCheckService = new TestDcpDependencyCheckService
        {
            // Container installed but not running - unhealthy state
            DcpInfoResult = new DcpInfo
            {
                Containers = new DcpContainersInfo
                {
                    Installed = true,
                    Running = false,
                    Error = "Podman is not running"
                }
            }
        };
        var interactionService = new TestInteractionService { IsAvailable = true };
        var locations = new Locations();
        var timeProvider = new FakeTimeProvider();

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        // Act
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None).DefaultTimeout();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);

        // Assert
        Assert.Equal(InteractionStrings.ContainerRuntimeUnhealthyTitle, interaction.Title);
        Assert.Contains("podman", interaction.Message);
        Assert.Contains(InteractionStrings.ContainerRuntimePodmanAdvice, interaction.Message);
        var notificationOptions = Assert.IsType<NotificationInteractionOptions>(interaction.Options);
        Assert.Equal(MessageIntent.Error, notificationOptions.Intent);
        Assert.Null(notificationOptions.LinkUrl); // No specific link for Podman
    }

    [Fact]
    public async Task DcpHost_WithUnhealthyContainerRuntime_NotificationCancelledWhenRuntimeBecomesHealthy()
    {
        // Arrange - this test verifies that the notification is cancelled when runtime becomes healthy
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker", CliPath = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh" });
        var dependencyCheckService = new TestDcpDependencyCheckService
        {
            // Initially unhealthy
            DcpInfoResult = new DcpInfo
            {
                Containers = new DcpContainersInfo
                {
                    Installed = true,
                    Running = false,
                    Error = "Docker daemon is not running"
                }
            }
        };
        var interactionService = new TestInteractionService { IsAvailable = true };
        var locations = new Locations();
        var timeProvider = new FakeTimeProvider();

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        // Act
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None).DefaultTimeout();

        // Use ReadAsync with timeout to wait for the notification
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);
        
        // Assert - Verify notification was shown initially
        Assert.Equal(InteractionStrings.ContainerRuntimeUnhealthyTitle, interaction.Title);
        Assert.False(interaction.CancellationToken.IsCancellationRequested); // Should not be cancelled yet

        // Simulate container runtime becoming healthy
        dependencyCheckService.DcpInfoResult = new DcpInfo
        {
            Containers = new DcpContainersInfo
            {
                Installed = true,
                Running = true,
                Error = null
            }
        };

        // Advance time by 5 seconds to trigger the next polling cycle
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Assert - The notification should now be cancelled
        using (var testTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(interaction.CancellationToken, testTimeoutCts.Token))
        {
            await Assert.ThrowsAsync<TaskCanceledException>(() => Task.Delay(-1, linkedCts.Token));
        }
}

    [Fact]
    public async Task DcpHost_WithContainerRuntimeNotInstalled_ShowsNotificationWithoutPolling()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker", CliPath = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh" });
        var dependencyCheckService = new TestDcpDependencyCheckService
        {
            // Container not installed
            DcpInfoResult = new DcpInfo
            {
                Containers = new DcpContainersInfo
                {
                    Installed = false,
                    Running = false,
                    Error = "No container runtime found"
                }
            }
        };
        var interactionService = new TestInteractionService { IsAvailable = true };
        var locations = new Locations();
        var timeProvider = new FakeTimeProvider();

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel,
            timeProvider);

        // Act
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None).DefaultTimeout();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);

        // Assert
        Assert.Equal(InteractionStrings.ContainerRuntimeNotInstalledTitle, interaction.Title);
        Assert.Contains(InteractionStrings.ContainerRuntimeNotInstalledMessage, interaction.Message);
        Assert.Contains("https://aka.ms/dotnet/aspire/containers", interaction.Message);
        var notificationOptions = Assert.IsType<NotificationInteractionOptions>(interaction.Options);
        Assert.Equal(MessageIntent.Error, notificationOptions.Intent);
        Assert.Equal(InteractionStrings.ContainerRuntimeLinkText, notificationOptions.LinkText);
        Assert.Equal("https://aka.ms/dotnet/aspire/containers", notificationOptions.LinkUrl);

        // Verify that no polling is started by ensuring the cancellation token is not cancelled after a delay
        // This tests that the function returns immediately and doesn't start the polling task
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        Assert.False(interaction.CancellationToken.IsCancellationRequested);
    }

    private static DistributedApplication CreateAppWithContainers()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("test-container", "nginx:latest");
        return builder.Build();
    }

    private sealed class TestDcpDependencyCheckService : IDcpDependencyCheckService
    {
        public DcpInfo? DcpInfoResult { get; set; } = new DcpInfo
        {
            VersionString = DcpVersion.Dev.ToString(),
            Version = DcpVersion.Dev,
            Containers = new DcpContainersInfo
            {
                Runtime = "docker",
                Installed = true,
                Running = true
            }
        };

        public Task<DcpInfo?> GetDcpInfoAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DcpInfoResult);
        }
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
