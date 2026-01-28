// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Resources;
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
