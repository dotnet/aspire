// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ClientSideBlazorBuilderTests
{
    [Fact]
    public void Constructor_NullProjectBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSerializer = new Mock<IServiceDiscoveryInfoSerializer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClientSideBlazorBuilder<Projects.TestProject_BlazorApp_Client>(null!, "wasmclient", mockSerializer.Object));
    }

    [Fact]
    public void Constructor_NullResourceName_ThrowsArgumentNullException()
    {
        // Arrange
        var mockSerializer = new Mock<IServiceDiscoveryInfoSerializer>();
        var mockBuilder = new Mock<IResourceBuilder<ProjectResource>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClientSideBlazorBuilder<Projects.TestProject_BlazorApp_Client>(mockBuilder.Object, null!, mockSerializer.Object));
    }

    [Fact]
    public void Constructor_EmptyResourceName_ThrowsArgumentException()
    {
        // Arrange
        var mockSerializer = new Mock<IServiceDiscoveryInfoSerializer>();
        var mockBuilder = new Mock<IResourceBuilder<ProjectResource>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ClientSideBlazorBuilder<Projects.TestProject_BlazorApp_Client>(mockBuilder.Object, string.Empty, mockSerializer.Object));
    }

    [Fact]
    public void Constructor_WhitespaceResourceName_ThrowsArgumentException()
    {
        // Arrange
        var mockSerializer = new Mock<IServiceDiscoveryInfoSerializer>();
        var mockBuilder = new Mock<IResourceBuilder<ProjectResource>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ClientSideBlazorBuilder<Projects.TestProject_BlazorApp_Client>(mockBuilder.Object, " ", mockSerializer.Object));
    }

    [Fact]
    public void Constructor_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var mockProjectBuilder = new Mock<IResourceBuilder<ProjectResource>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClientSideBlazorBuilder<Projects.TestProject_BlazorApp_Client>(mockProjectBuilder.Object, "wasmclient", null!));
    }
}
