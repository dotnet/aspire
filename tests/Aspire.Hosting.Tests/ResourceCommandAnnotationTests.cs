// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceCommandAnnotationTests
{
    [Theory]
    [InlineData(KnownResourceCommands.StartCommand, "Starting", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.StartCommand, "Stopping", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StartCommand, "Running", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StartCommand, "Exited", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.StartCommand, "Finished", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.StartCommand, "FailedToStart", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.StartCommand, "Unknown", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.StartCommand, "Waiting", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.StartCommand, "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.StartCommand, "", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StartCommand, null, ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "Starting", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "Stopping", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.StopCommand, "Running", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.StopCommand, "Exited", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "Finished", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "FailedToStart", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "Unknown", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "Waiting", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "RuntimeUnhealthy", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, "", ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.StopCommand, null, ResourceCommandState.Hidden)]
    [InlineData(KnownResourceCommands.RestartCommand, "Starting", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "Stopping", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "Running", ResourceCommandState.Enabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "Exited", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "Finished", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "FailedToStart", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "Unknown", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "Waiting", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, "", ResourceCommandState.Disabled)]
    [InlineData(KnownResourceCommands.RestartCommand, null, ResourceCommandState.Disabled)]
    public void LifeCycleCommands_CommandState(string commandName, string? resourceState, ResourceCommandState commandState)
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
}
