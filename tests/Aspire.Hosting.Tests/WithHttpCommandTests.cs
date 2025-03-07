// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithHttpCommandTests
{
    private static readonly TimeSpan s_startTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_healthyTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithDefaultValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing");

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("Do The Thing", command.DisplayName);
        // Expected name format: "{endpoint.Resource.Name}-{endpoint.EndpointName}-http-{httpMethod}"
        Assert.Equal($"{resourceBuilder.Resource.Name}-http-http-post", command.Name);
        Assert.Null(command.DisplayDescription);
        Assert.Null(command.ConfirmationMessage);
        Assert.Null(command.IconName);
        Assert.Null(command.IconVariant);
        Assert.False(command.IsHighlighted);
    }

    [Fact]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithCustomValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing",
                commandName: "my-command-name",
                displayDescription: "Command description",
                confirmationMessage: "Are you sure?",
                iconName: "DatabaseLightning",
                iconVariant: IconVariant.Filled,
                isHighlighted: true);

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("Do The Thing", command.DisplayName);
        Assert.Equal("my-command-name", command.Name);
        Assert.Equal("Command description", command.DisplayDescription);
        Assert.Equal("Are you sure?", command.ConfirmationMessage);
        Assert.Equal("DatabaseLightning", command.IconName);
        Assert.Equal(IconVariant.Filled, command.IconVariant);
        Assert.True(command.IsHighlighted);
    }

    [Fact]
    public async Task WithHttpCommand_EnablesCommandOnceResourceIsRunning()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        var markHealthy = false;
        builder.Services.AddHealthChecks()
            .AddCheck("ManualHealthCheck", () => markHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Not ready"));

        builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing", commandName: "mycommand")
            .WithHealthCheck("ManualHealthCheck");

        using var app = builder.Build();
        ResourceCommandState? commandState = null;
        var watchCts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(watchCts.Token).WithCancellation(watchCts.Token))
            {
                var commandSnapshot = resourceEvent.Snapshot.Commands.First(c => c.Name == "mycommand");
                commandState = commandSnapshot.State;
            }
        }, watchCts.Token);

        // Act/Assert
        await app.StartAsync().WaitAsync(s_startTimeout);

        await app.ResourceNotifications.WaitForResourceAsync("servicea", KnownResourceStates.Starting).WaitAsync(s_startTimeout);

        Assert.Equal(ResourceCommandState.Disabled, commandState);

        markHealthy = true;

        var resourceEvent = await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").WaitAsync(s_healthyTimeout);

        Assert.Equal(ResourceCommandState.Enabled, commandState);

        // Clean up
        watchCts.Cancel();
        await app.StopAsync();
    }
}
