// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class DockerfileBuildCallbackContextTests
{
    [Fact]
    public void DockerfileBuildCallbackContext_Constructor_SetsProperties()
    {
        // Arrange & Act
        var context = new DockerfileBuildCallbackContext("alpine", "latest", "/app", "production");

        // Assert
        Assert.Equal("alpine", context.BaseStageRepository);
        Assert.Equal("latest", context.BaseStageTag);
        Assert.Equal("/app", context.DefaultContextPath);
        Assert.Equal("production", context.TargetStage);
    }

    [Fact]
    public void DockerfileBuildCallbackContext_Constructor_WithNullValues_AllowsNulls()
    {
        // Arrange & Act
        var context = new DockerfileBuildCallbackContext("node", null, "/src", null);

        // Assert
        Assert.Equal("node", context.BaseStageRepository);
        Assert.Null(context.BaseStageTag);
        Assert.Equal("/src", context.DefaultContextPath);
        Assert.Null(context.TargetStage);
    }

    [Fact]
    public void DockerfileBuildCallbackContext_Properties_AreReadOnly()
    {
        // Arrange
        var context = new DockerfileBuildCallbackContext("ubuntu", "20.04", "/build", "final");

        // Act & Assert - Properties should have getters only
        Assert.Equal("ubuntu", context.BaseStageRepository);
        Assert.Equal("20.04", context.BaseStageTag);
        Assert.Equal("/build", context.DefaultContextPath);
        Assert.Equal("final", context.TargetStage);

        // Verify properties are read-only by checking type info
        var properties = typeof(DockerfileBuildCallbackContext).GetProperties();
        foreach (var property in properties)
        {
            Assert.True(property.CanRead, $"Property {property.Name} should be readable");
            Assert.False(property.CanWrite, $"Property {property.Name} should be read-only");
        }
    }
}