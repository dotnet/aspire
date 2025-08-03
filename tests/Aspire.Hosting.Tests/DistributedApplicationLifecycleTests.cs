// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationLifecycleTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task StartedAsync_WithoutDashboard_ShowsMessageImmediately()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddProvider(new TestLoggerProvider(testSink));
            b.AddXunit(testOutputHelper);
        });

        var logger = loggerFactory.CreateLogger<DistributedApplication>();
        var configuration = new ConfigurationBuilder().Build();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var applicationModel = new DistributedApplicationModel(new ResourceCollection());
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var lifecycle = new DistributedApplicationLifecycle(logger, configuration, executionContext, applicationModel, resourceNotificationService);

        // Act
        await lifecycle.StartedAsync(CancellationToken.None);

        // Assert
        var logEntry = testSink.Writes.FirstOrDefault(w => w.Message == "Distributed application started. Press Ctrl+C to shut down.");
        Assert.NotNull(logEntry);
    }

    [Fact]
    public async Task StartedAsync_NotInRunMode_DoesNotShowMessage()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddProvider(new TestLoggerProvider(testSink));
            b.AddXunit(testOutputHelper);
        });

        var logger = loggerFactory.CreateLogger<DistributedApplication>();
        var configuration = new ConfigurationBuilder().Build();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var applicationModel = new DistributedApplicationModel(new ResourceCollection());
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var lifecycle = new DistributedApplicationLifecycle(logger, configuration, executionContext, applicationModel, resourceNotificationService);

        // Act
        await lifecycle.StartedAsync(CancellationToken.None);

        // Assert
        var logEntry = testSink.Writes.FirstOrDefault(w => w.Message == "Distributed application started. Press Ctrl+C to shut down.");
        Assert.Null(logEntry);
    }

    [Fact]
    public async Task StartedAsync_WithDashboard_WaitsForDashboardHealthy()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddProvider(new TestLoggerProvider(testSink));
            b.AddXunit(testOutputHelper);
        });

        var logger = loggerFactory.CreateLogger<DistributedApplication>();
        var configuration = new ConfigurationBuilder().Build();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        
        // Create a dashboard resource
        var resources = new ResourceCollection();
        var dashboardResource = new ExecutableResource(KnownResourceNames.AspireDashboard, "aspire-dashboard", ".");
        resources.Add(dashboardResource);
        var applicationModel = new DistributedApplicationModel(resources);
        
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var lifecycle = new DistributedApplicationLifecycle(logger, configuration, executionContext, applicationModel, resourceNotificationService);

        // Set up a task to mark the dashboard as healthy after a short delay
        var healthyTask = Task.Run(async () =>
        {
            await Task.Delay(50); // Small delay to ensure StartedAsync starts waiting first
            await resourceNotificationService.PublishUpdateAsync(dashboardResource, s => s with 
            { 
                State = new ResourceStateSnapshot(KnownResourceStates.Running, null)
            });
        });

        // Act
        var startedTask = lifecycle.StartedAsync(CancellationToken.None);
        
        // Wait for both tasks to complete
        await Task.WhenAll(startedTask, healthyTask);

        // Assert
        var logEntry = testSink.Writes.FirstOrDefault(w => w.Message == "Distributed application started. Press Ctrl+C to shut down.");
        Assert.NotNull(logEntry);
    }

    [Fact]
    public async Task StartedAsync_WithDashboardButWaitFails_StillShowsMessage()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddProvider(new TestLoggerProvider(testSink));
            b.AddXunit(testOutputHelper);
        });

        var logger = loggerFactory.CreateLogger<DistributedApplication>();
        var configuration = new ConfigurationBuilder().Build();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        
        // Create a dashboard resource
        var resources = new ResourceCollection();
        var dashboardResource = new ExecutableResource(KnownResourceNames.AspireDashboard, "aspire-dashboard", ".");
        resources.Add(dashboardResource);
        var applicationModel = new DistributedApplicationModel(resources);
        
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var lifecycle = new DistributedApplicationLifecycle(logger, configuration, executionContext, applicationModel, resourceNotificationService);

        // Use a cancellation token that will be cancelled to simulate a wait failure
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act
        await lifecycle.StartedAsync(cts.Token);

        // Assert
        var logEntry = testSink.Writes.FirstOrDefault(w => w.Message == "Distributed application started. Press Ctrl+C to shut down.");
        Assert.NotNull(logEntry);
    }
}