// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
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
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None);

        // Use ReadAsync with timeout instead of Task.Delay and TryRead
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);

        // Assert
        Assert.Equal("Container Runtime Unhealthy", interaction.Title);
        Assert.Contains("docker", interaction.Message);
        Assert.Contains("unhealthy", interaction.Message);
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
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None);

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
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None);

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
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None);

        // Use ReadAsync with timeout instead of Task.Delay and TryRead
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);

        // Assert
        Assert.Equal("Container Runtime Unhealthy", interaction.Title);
        Assert.Contains("podman", interaction.Message);
        Assert.Contains("Ensure that Podman is running", interaction.Message);
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
        await dcpHost.EnsureDcpContainerRuntimeAsync(CancellationToken.None);

        // Use ReadAsync with timeout to wait for the notification
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var interaction = await interactionService.Interactions.Reader.ReadAsync(cts.Token);
        
        // Assert - Verify notification was shown initially
        Assert.Equal("Container Runtime Unhealthy", interaction.Title);
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

        // Advance time by 10 seconds to trigger the next polling cycle
        timeProvider.Advance(TimeSpan.FromSeconds(10));

        // Give a moment for the background task to process the health check and cancel the notification
        await Task.Delay(50);
        
        // Assert - The notification token should now be cancelled
        Assert.True(interaction.CancellationToken.IsCancellationRequested);
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