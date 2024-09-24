// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceCommandAnnotationTests
{
    [Theory]
    [InlineData(CommandsConfigurationExtensions.StartType, "Starting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StartType, "Stopping", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StartType, "Running", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StartType, "Exited", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StartType, "Finished", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StartType, "FailedToStart", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StartType, "Waiting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StopType, "Starting", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopType, "Stopping", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.StopType, "Running", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.StopType, "Exited", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopType, "Finished", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopType, "FailedToStart", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.StopType, "Waiting", ResourceCommandState.Hidden)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "Starting", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "Stopping", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "Running", ResourceCommandState.Enabled)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "Exited", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "Finished", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "FailedToStart", ResourceCommandState.Disabled)]
    [InlineData(CommandsConfigurationExtensions.RestartType, "Waiting", ResourceCommandState.Disabled)]
    public void LifeCycleCommands_CommandState(string commandType, string resourceState, ResourceCommandState commandState)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("name", "image");
        resourceBuilder.Resource.AddLifeCycleCommands();

        var startCommand = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().Single(a => a.Type == commandType);

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
