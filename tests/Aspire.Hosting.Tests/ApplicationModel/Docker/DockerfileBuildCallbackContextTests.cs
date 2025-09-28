// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ApplicationModel.Docker;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class DockerfileBuildCallbackContextTests
{
    [Fact]
    public void DockerfileBuildCallbackContext_Constructor_SetsProperties()
    {
        // Arrange
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var context = new DockerfileBuildCallbackContext("alpine", "latest", "/app", "production", builder, services);

        // Assert
        Assert.Equal("alpine", context.BaseStageRepository);
        Assert.Equal("latest", context.BaseStageTag);
        Assert.Equal("/app", context.DefaultContextPath);
        Assert.Equal("production", context.TargetStage);
        Assert.Same(builder, context.Builder);
        Assert.Same(services, context.Services);
    }

    [Fact]
    public void DockerfileBuildCallbackContext_Constructor_WithNullValues_AllowsNulls()
    {
        // Arrange
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var context = new DockerfileBuildCallbackContext("node", null, "/src", null, builder, services);

        // Assert
        Assert.Equal("node", context.BaseStageRepository);
        Assert.Null(context.BaseStageTag);
        Assert.Equal("/src", context.DefaultContextPath);
        Assert.Null(context.TargetStage);
        Assert.Same(builder, context.Builder);
        Assert.Same(services, context.Services);
    }

    [Fact]
    public void DockerfileBuildCallbackContext_Properties_AreReadOnly()
    {
        // Arrange
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DockerfileBuildCallbackContext("ubuntu", "20.04", "/build", "final", builder, services);

        // Act & Assert - Properties should have getters only
        Assert.Equal("ubuntu", context.BaseStageRepository);
        Assert.Equal("20.04", context.BaseStageTag);
        Assert.Equal("/build", context.DefaultContextPath);
        Assert.Equal("final", context.TargetStage);
        Assert.Same(builder, context.Builder);
        Assert.Same(services, context.Services);

        // Verify properties are read-only by checking type info
        var properties = typeof(DockerfileBuildCallbackContext).GetProperties();
        foreach (var property in properties)
        {
            Assert.True(property.CanRead, $"Property {property.Name} should be readable");
            Assert.False(property.CanWrite, $"Property {property.Name} should be read-only");
        }
    }

    [Fact]
    public void DockerfileBuildCallbackContext_Builder_CanBeUsedToModifyDockerfile()
    {
        // Arrange
        var builder = new DockerfileBuilder();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DockerfileBuildCallbackContext("alpine", "latest", "/app", "production", builder, services);

        // Act
        context.Builder.From("node", "18")
            .WorkDir("/app")
            .Run("npm install");

        // Assert
        Assert.Single(context.Builder.Stages);
        Assert.Equal(3, context.Builder.Stages[0].Statements.Count); // FROM + WORKDIR + RUN
    }

    [Fact]
    public void DockerfileBuildCallbackContext_Services_CanBeUsedForDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<string>("test-service")
            .BuildServiceProvider();
        var builder = new DockerfileBuilder();
        var context = new DockerfileBuildCallbackContext("alpine", "latest", "/app", "production", builder, services);

        // Act
        var retrievedService = context.Services.GetService<string>();

        // Assert
        Assert.Equal("test-service", retrievedService);
    }
}