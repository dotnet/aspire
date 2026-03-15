// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

[Trait("Partition", "2")]
public class ResourceCommandAnnotationTests
{
    [Theory]
    [InlineData("start", "Starting", ResourceCommandState.Disabled)]
    [InlineData("start", "Stopping", ResourceCommandState.Hidden)]
    [InlineData("start", "Running", ResourceCommandState.Hidden)]
    [InlineData("start", "Exited", ResourceCommandState.Enabled)]
    [InlineData("start", "Finished", ResourceCommandState.Enabled)]
    [InlineData("start", "FailedToStart", ResourceCommandState.Enabled)]
    [InlineData("start", "Unknown", ResourceCommandState.Enabled)]
    [InlineData("start", "Waiting", ResourceCommandState.Enabled)]
    [InlineData("start", "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData("start", "", ResourceCommandState.Disabled)]
    [InlineData("start", null, ResourceCommandState.Disabled)]
    [InlineData("stop", "Starting", ResourceCommandState.Hidden)]
    [InlineData("stop", "Stopping", ResourceCommandState.Disabled)]
    [InlineData("stop", "Running", ResourceCommandState.Enabled)]
    [InlineData("stop", "Exited", ResourceCommandState.Hidden)]
    [InlineData("stop", "Finished", ResourceCommandState.Hidden)]
    [InlineData("stop", "FailedToStart", ResourceCommandState.Hidden)]
    [InlineData("stop", "Unknown", ResourceCommandState.Hidden)]
    [InlineData("stop", "Waiting", ResourceCommandState.Hidden)]
    [InlineData("stop", "RuntimeUnhealthy", ResourceCommandState.Hidden)]
    [InlineData("stop", "", ResourceCommandState.Hidden)]
    [InlineData("stop", null, ResourceCommandState.Hidden)]
    [InlineData("restart", "Starting", ResourceCommandState.Disabled)]
    [InlineData("restart", "Stopping", ResourceCommandState.Disabled)]
    [InlineData("restart", "Running", ResourceCommandState.Enabled)]
    [InlineData("restart", "Exited", ResourceCommandState.Disabled)]
    [InlineData("restart", "Finished", ResourceCommandState.Disabled)]
    [InlineData("restart", "FailedToStart", ResourceCommandState.Disabled)]
    [InlineData("restart", "Unknown", ResourceCommandState.Disabled)]
    [InlineData("restart", "Waiting", ResourceCommandState.Disabled)]
    [InlineData("restart", "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [InlineData("restart", "", ResourceCommandState.Disabled)]
    [InlineData("restart", null, ResourceCommandState.Disabled)]
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

    [Fact]
    public void RestartCommand_ContainerResource_HasGenericDescription()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("name", "image");
        resourceBuilder.Resource.AddLifeCycleCommands();

        // Act
        var restartCommand = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().Single(a => a.Name == KnownResourceCommands.RestartCommand);

        // Assert - Container resources should have the generic description
        Assert.Equal(CommandStrings.RestartDescription, restartCommand.DisplayDescription);
    }

    [Fact]
    public void RestartCommand_ProjectResource_HasDetailedDescription()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var projectResource = new ProjectResource("testproject");
        projectResource.AddLifeCycleCommands();

        // Act
        var restartCommand = projectResource.Annotations.OfType<ResourceCommandAnnotation>().Single(a => a.Name == KnownResourceCommands.RestartCommand);

        // Assert - Project resources should have the detailed description mentioning source code is not recompiled
        Assert.Equal(CommandStrings.RestartProjectDescription, restartCommand.DisplayDescription);
    }

    [Fact]
    public void RestartCommand_CSharpAppResource_HasDetailedDescription()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var csharpAppResource = new CSharpAppResource("testapp");
        csharpAppResource.AddLifeCycleCommands();

        // Act
        var restartCommand = csharpAppResource.Annotations.OfType<ResourceCommandAnnotation>().Single(a => a.Name == KnownResourceCommands.RestartCommand);

        // Assert - Single file C# app resources should have the detailed description mentioning source code is not recompiled
        Assert.Equal(CommandStrings.RestartProjectDescription, restartCommand.DisplayDescription);
    }
}
