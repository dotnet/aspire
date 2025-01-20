// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Testing;
using Moq;
using Projects;
using Xunit;

namespace Aspire.Hosting.Tests;

public class JsonServiceDiscoveryInfoSerializerTests
{
    [Fact]
    public void Constructor_NullFileAccessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JsonServiceDiscoveryInfoSerializer(null!));
    }

    [Fact]
    public async Task SerializeServiceDiscoveryInfo_EndpointsNotYetAllocated_ThrowsInvalidOperationException()
    {
        // Arrange
        var distributedApplicationBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TestProject_AppHost>();
        var resourceBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("TestResource"))
            .WithEndpoint(1, 1, "https", "TestEndpoint", distributedApplicationBuilder.Environment.EnvironmentName, false, false);

        var mockFileAccessor = new Mock<IJsonFileAccessor>();
        mockFileAccessor.Setup(a => a.ReadFileAsJson()).Returns(new JsonObject());

        var serializer = new JsonServiceDiscoveryInfoSerializer(mockFileAccessor.Object);
        Assert.Throws<InvalidOperationException>(() => serializer.SerializeServiceDiscoveryInfo(resourceBuilder.Resource));
    }

    [Fact]
    public async Task SerializeServiceDiscoveryInfo_ValidResource_SavesCorrectJson()
    {
        // Arrange
        var distributedApplicationBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TestProject_AppHost>();
        var resourceBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("TestResource"))
            .WithEndpoint(1, 1, "https", "TestEndpoint", distributedApplicationBuilder.Environment.EnvironmentName, false, false);
        var resource = resourceBuilder.Resource;
        var endpointAnnotation = resource.Annotations.OfType<EndpointAnnotation>().First(e => e.Name == "TestEndpoint");

        endpointAnnotation.AllocatedEndpoint = new AllocatedEndpoint(endpointAnnotation, "TestEndpoint", 1);

        var json = new JsonObject();
        var mockFileAccessor = new Mock<IJsonFileAccessor>();
        mockFileAccessor.Setup(fa => fa.ReadFileAsJson()).Returns(json);

        var serializer = new JsonServiceDiscoveryInfoSerializer(mockFileAccessor.Object);
        var jsonNodeOptions = new JsonNodeOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        var expectedJson = JsonNode.Parse("{\"Services\":{\"TestResource\":{\"TestEndpoint\":[\"https://TestEndpoint:1\"]}}}", jsonNodeOptions)!.AsObject();

        // Act
        serializer.SerializeServiceDiscoveryInfo(resource);

        // Assert
        mockFileAccessor.Verify(fa => fa.SaveJson(It.Is<JsonObject>(j => j.ToJsonString(jsonSerializerOptions).Equals(expectedJson.ToJsonString(jsonSerializerOptions)))));
    }

    [Fact]
    public async Task SerializeServiceDiscoveryInfo_ResourceWithNoEndpoints_SavesCorrectJson()
    {
        // Arrange
        var distributedApplicationBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TestProject_AppHost>();
        var resourceBuilder = distributedApplicationBuilder.CreateResourceBuilder(new ProjectResource("TestResource"));
        var mockFileAccessor = new Mock<IJsonFileAccessor>();

        var json = new JsonObject();
        mockFileAccessor.Setup(fa => fa.ReadFileAsJson()).Returns(json);

        var serializer = new JsonServiceDiscoveryInfoSerializer(mockFileAccessor.Object);

        // Act
        serializer.SerializeServiceDiscoveryInfo(resourceBuilder.Resource);

        // Assert
        mockFileAccessor.Verify(fa => fa.SaveJson(It.Is<JsonObject>(jo =>
            jo["Services"]!["TestResource"] != null &&
            jo["Services"]!["TestResource"]!.AsObject().Count == 0)));
    }
}
