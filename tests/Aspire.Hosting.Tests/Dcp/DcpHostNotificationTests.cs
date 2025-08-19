// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

        // Act & Assert - should not throw
        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel);

        Assert.NotNull(dcpHost);
    }

    [Fact]
    public async Task DcpHost_WithUnhealthyContainerRuntime_ShowsNotification()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker" });
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

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel);

        // Act
        await dcpHost.StartAsync(CancellationToken.None);

        // Wait a bit for the fire-and-forget notification to complete
        await Task.Delay(100);

        // Assert
        Assert.True(interactionService.Interactions.Reader.TryRead(out var interaction));
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
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker" });
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

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel);

        // Act
        await dcpHost.StartAsync(CancellationToken.None);

        // Wait a bit to ensure no notification is sent
        await Task.Delay(100);

        // Assert
        Assert.False(interactionService.Interactions.Reader.TryRead(out _));
    }

    [Fact]
    public async Task DcpHost_WithDashboardDisabled_DoesNotShowNotification()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "docker" });
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

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel);

        // Act
        await dcpHost.StartAsync(CancellationToken.None);

        // Wait a bit to ensure no notification is sent
        await Task.Delay(100);

        // Assert - no notification should be shown when dashboard is disabled
        Assert.False(interactionService.Interactions.Reader.TryRead(out _));
    }

    [Fact]
    public async Task DcpHost_WithPodmanUnhealthy_ShowsCorrectMessage()
    {
        // Arrange
        using var app = CreateAppWithContainers();
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        
        var loggerFactory = new NullLoggerFactory();
        var dcpOptions = Options.Create(new DcpOptions { ContainerRuntime = "podman" });
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

        var dcpHost = new DcpHost(
            loggerFactory,
            dcpOptions,
            dependencyCheckService,
            interactionService,
            locations,
            applicationModel);

        // Act
        await dcpHost.StartAsync(CancellationToken.None);

        // Wait a bit for the fire-and-forget notification to complete
        await Task.Delay(100);

        // Assert
        Assert.True(interactionService.Interactions.Reader.TryRead(out var interaction));
        Assert.Equal("Container Runtime Unhealthy", interaction.Title);
        Assert.Contains("podman", interaction.Message);
        Assert.Contains("Ensure that Podman is running", interaction.Message);
        var notificationOptions = Assert.IsType<NotificationInteractionOptions>(interaction.Options);
        Assert.Equal(MessageIntent.Error, notificationOptions.Intent);
        Assert.Null(notificationOptions.LinkUrl); // No specific link for Podman
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