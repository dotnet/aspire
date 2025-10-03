// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class ResourceCommandAnnotationTests
{
    [Theory]
    [InlineData("resource-start", "Starting", ResourceCommandState.Disabled)]
    [InlineData("resource-start", "Stopping", ResourceCommandState.Hidden)]
    [InlineData("resource-start", "Running", ResourceCommandState.Hidden)]
    [InlineData("resource-start", "Exited", ResourceCommandState.Enabled)]
    [InlineData("resource-start", "Finished", ResourceCommandState.Enabled)]
    [InlineData("resource-start", "FailedToStart", ResourceCommandState.Enabled)]
    [InlineData("resource-start", "Unknown", ResourceCommandState.Enabled)]
    [InlineData("resource-start", "Waiting", ResourceCommandState.Enabled)]
    [InlineData("resource-start", "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData("resource-start", "", ResourceCommandState.Disabled)]
    [InlineData("resource-start", null, ResourceCommandState.Disabled)]
    [InlineData("resource-stop", "Starting", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "Stopping", ResourceCommandState.Disabled)]
    [InlineData("resource-stop", "Running", ResourceCommandState.Enabled)]
    [InlineData("resource-stop", "Exited", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "Finished", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "FailedToStart", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "Unknown", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "Waiting", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "RuntimeUnhealthy", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", "", ResourceCommandState.Hidden)]
    [InlineData("resource-stop", null, ResourceCommandState.Hidden)]
    [InlineData("resource-restart", "Starting", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "Stopping", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "Running", ResourceCommandState.Enabled)]
    [InlineData("resource-restart", "Exited", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "Finished", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "FailedToStart", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "Unknown", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "Waiting", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", "", ResourceCommandState.Disabled)]
    [InlineData("resource-restart", null, ResourceCommandState.Disabled)]
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
