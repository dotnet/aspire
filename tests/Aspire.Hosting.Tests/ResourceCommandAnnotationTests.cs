// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceCommandAnnotationTests
{
    [Theory]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "Starting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "Stopping", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "Running", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "Exited", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "Finished", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "FailedToStart", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "Waiting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StartCommandName, "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "Starting", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "Stopping", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "Running", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "Exited", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "Finished", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "FailedToStart", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "Waiting", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopCommandName, "RuntimeUnhealthy", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "Starting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "Stopping", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "Running", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "Exited", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "Finished", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "FailedToStart", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "Waiting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartCommandName, "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    public void LifeCycleCommands_CommandState(string commandName, string resourceState, ResourceCommandState commandState)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("name", "image");
        resourceBuilder.Resource.AddLifeCycleCommands();

        var startCommand = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().Single(a => a.Name == commandName);

        // Act
        var state = startCommand.UpdateState(new UpdateCommandStateContext
        {
            ResourceSnapshot = new CustomResourceSnapshot
            {
                Properties = [],
                ResourceType = "test",
                State = resourceState
            },
            ServiceProvider = new ServiceCollection().BuildServiceProvider()
        });

        // Assert
        Assert.Equal(commandState, state);
    }

    [Fact]
    public async Task RunLifeCycleCommandAsync_Match_Success()
    {
        // Arrange
        var testResource = new TestResource("test-resource-name");
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var services = new ServiceCollection();
        services.AddSingleton(resourceNotificationService);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var task = CommandsConfigurationExtensions.RunLifeCycleCommandAsync(
            (serviceProvider, ct) =>
            {
                return Task.CompletedTask;
            },
            serviceProvider,
            "test-resource-name",
            state => state == "Running",
            TimeSpan.FromSeconds(1),
            CancellationToken.None
        );

        Assert.False(task.IsCompletedSuccessfully);

        await resourceNotificationService.PublishUpdateAsync(
            testResource,
            "test-resource-name",
            s => s with { State = "Running" }).DefaultTimeout();

        // Assert
        var result = await task.DefaultTimeout();
        Assert.True(result.Success);
    }

    [Fact]
    public async Task RunLifeCycleCommandAsync_NoMatch_Failure()
    {
        // Arrange
        var testResource = new TestResource("test-resource-name");
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var services = new ServiceCollection();
        services.AddSingleton(resourceNotificationService);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var task = CommandsConfigurationExtensions.RunLifeCycleCommandAsync(
            (serviceProvider, ct) =>
            {
                return Task.CompletedTask;
            },
            serviceProvider,
            "test-resource-name",
            state => state == "Running",
            TimeSpan.FromSeconds(1),
            CancellationToken.None
        );

        await resourceNotificationService.PublishUpdateAsync(
            testResource,
            "test-resource-name",
            s => s with { State = "FailedToStart" }).DefaultTimeout();

        // Assert
        var result = await task.DefaultTimeout();
        Assert.False(result.Success);
    }

    private sealed class TestResource(string resourceName) : Resource(resourceName)
    {
    }
}
