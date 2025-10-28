// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Tests.Shared.Telemetry;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Mcp;

public class AspireTelemetryMcpToolsTests
{
    [Fact]
    public void ListTraces_NoResources_ReturnsEmptyResult()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraces(resourceName: null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
    }

    [Fact]
    public void ListTraces_SingleResource_ReturnsTraces()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraces(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
    }

    [Fact]
    public void ListTraces_MultipleResourcesWithSameName_HandlesGracefully()
    {
        // Arrange
        var repository = CreateRepository();
        // Add multiple resources with the same name but different instance IDs
        AddResource(repository, "app1", instanceId: "instance1");
        AddResource(repository, "app1", instanceId: "instance2");
        var tools = CreateTools(repository);

        // Act - This should not throw even though there are multiple matches
        var result = tools.ListTraces(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        // When there are multiple resources with the same name, the method should return an error message
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void ListTraces_ResourceNotFound_ReturnsErrorMessage()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraces(resourceName: "nonexistent");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void ListStructuredLogs_NoResources_ReturnsEmptyResult()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(resourceName: null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
    }

    [Fact]
    public void ListStructuredLogs_SingleResource_ReturnsLogs()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
    }

    [Fact]
    public void ListStructuredLogs_MultipleResourcesWithSameName_HandlesGracefully()
    {
        // Arrange
        var repository = CreateRepository();
        // Add multiple resources with the same name but different instance IDs
        AddResource(repository, "app1", instanceId: "instance1");
        AddResource(repository, "app1", instanceId: "instance2");
        var tools = CreateTools(repository);

        // Act - This should not throw even though there are multiple matches
        var result = tools.ListStructuredLogs(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        // When there are multiple resources with the same name, the method should return an error message
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void ListTraceStructuredLogs_WithTraceId_ReturnsLogs()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraceStructuredLogs(traceId: "test-trace-id");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
    }

    private static AspireTelemetryMcpTools CreateTools(TelemetryRepository repository)
    {
        return new AspireTelemetryMcpTools(repository, []);
    }

    private static TelemetryRepository CreateRepository()
    {
        return TelemetryTestHelpers.CreateRepository();
    }

    private static void AddResource(TelemetryRepository repository, string name, string? instanceId = null)
    {
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: name, instanceId: instanceId)
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }
}
