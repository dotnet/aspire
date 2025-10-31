// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Tests.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Tests.Shared.DashboardModel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Mcp;

public class AspireResourceMcpToolsTests
{
    private static readonly ResourcePropertyViewModel s_excludeFromMcpProperty = new ResourcePropertyViewModel(KnownProperties.Resource.ExcludeFromMcp, Value.ForBool(true), isValueSensitive: false, knownProperty: null, priority: 0);

    [Fact]
    public void ListResources_NoResources_ReturnsResourceData()
    {
        // Arrange
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: []);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = tools.ListResources();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# RESOURCE DATA", result);
    }

    [Fact]
    public void ListResources_SingleResource_ReturnsResourceData()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceName: "app1");
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = tools.ListResources();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# RESOURCE DATA", result);
        Assert.Contains("app1", result);
    }

    [Fact]
    public void ListResources_MultipleResources_ReturnsAllResources()
    {
        // Arrange
        var resource1 = ModelTestHelpers.CreateResource(resourceName: "app1");
        var resource2 = ModelTestHelpers.CreateResource(resourceName: "app2");
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource1, resource2]);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = tools.ListResources();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# RESOURCE DATA", result);
        Assert.Contains("app1", result);
        Assert.Contains("app2", result);
    }

    [Fact]
    public void ListResources_OptOutResources_FiltersOptOutResources()
    {
        // Arrange
        var resource1 = ModelTestHelpers.CreateResource(resourceName: "app1");
        var resource2 = ModelTestHelpers.CreateResource(
            resourceName: "app2",
            properties: new Dictionary<string, ResourcePropertyViewModel> { [KnownProperties.Resource.ExcludeFromMcp] = s_excludeFromMcpProperty });
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource1, resource2]);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = tools.ListResources();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# RESOURCE DATA", result);
        Assert.Contains("app1", result);
        Assert.DoesNotContain("app2", result);
    }

    [Fact]
    public async Task ListConsoleLogsAsync_ResourceNotFound_ReturnsErrorMessage()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceName: "app1");
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = await tools.ListConsoleLogsAsync("nonexistent", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Unable to find a resource named 'nonexistent'", result);
    }

    [Fact]
    public async Task ListConsoleLogsAsync_ResourceOptOut_ReturnsErrorMessage()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "app1",
            properties: new Dictionary<string, ResourcePropertyViewModel> { [KnownProperties.Resource.ExcludeFromMcp] = s_excludeFromMcpProperty });
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = await tools.ListConsoleLogsAsync("app1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Unable to find a resource named 'app1'", result);
    }

    [Fact]
    public async Task ListConsoleLogsAsync_ResourceFound_ReturnsLogs()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceName: "app1");
        var logsChannel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>();
        logsChannel.Writer.Complete();

        var dashboardClient = new TestDashboardClient(
            isEnabled: true,
            initialResources: [resource],
            consoleLogsChannelProvider: _ => logsChannel);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = await tools.ListConsoleLogsAsync("app1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# CONSOLE LOGS", result);
    }

    [Fact]
    public async Task ListConsoleLogsAsync_MultipleResourcesWithSameName_HandlesGracefully()
    {
        // Arrange
        // When there are multiple resources with same name, GetResources returns them but
        // TryGetResource should return false since Count != 1
        var resource1 = ModelTestHelpers.CreateResource(resourceName: "app1");
        var resource2 = ModelTestHelpers.CreateResource(resourceName: "app1"); // Same name
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource1, resource2]);
        var tools = CreateTools(dashboardClient);

        // Act
        var result = await tools.ListConsoleLogsAsync("app1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Should return error message when multiple resources match
        Assert.Contains("Unable to find a resource named 'app1'", result);
    }

    [Fact]
    public async Task ExecuteResourceCommand_ResourceNotFound_ThrowsMcpProtocolException()
    {
        // Arrange
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: []);
        var tools = CreateTools(dashboardClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModelContextProtocol.McpProtocolException>(
            async () => await tools.ExecuteResourceCommand("nonexistent", "start"));

        Assert.Contains("Resource 'nonexistent' not found", exception.Message);
    }

    [Fact]
    public async Task ExecuteResourceCommand_ResourceOptOut_ThrowsMcpProtocolException()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(
            resourceName: "app1",
            commands: ImmutableArray<CommandViewModel>.Empty,
            properties: new Dictionary<string, ResourcePropertyViewModel> { [KnownProperties.Resource.ExcludeFromMcp] = s_excludeFromMcpProperty });
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);
        var tools = CreateTools(dashboardClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModelContextProtocol.McpProtocolException>(
            async () => await tools.ExecuteResourceCommand("app1", "start"));

        Assert.Contains("Resource 'app1' not found", exception.Message);
    }

    [Fact]
    public async Task ExecuteResourceCommand_CommandNotFound_ThrowsMcpProtocolException()
    {
        // Arrange
        var resource = ModelTestHelpers.CreateResource(resourceName: "app1", commands: ImmutableArray<CommandViewModel>.Empty);
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);
        var tools = CreateTools(dashboardClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModelContextProtocol.McpProtocolException>(
            async () => await tools.ExecuteResourceCommand("app1", "nonexistent-command"));

        Assert.Contains("Command 'nonexistent-command' not found", exception.Message);
    }

    private static AspireResourceMcpTools CreateTools(IDashboardClient dashboardClient)
    {
        var options = new DashboardOptions();
        options.Frontend.EndpointUrls = "https://localhost:1234";
        options.Frontend.PublicUrl = "https://localhost:8080";
        Assert.True(options.Frontend.TryParseOptions(out _));

        return new AspireResourceMcpTools(
            dashboardClient,
            new TestOptionsMonitor<DashboardOptions>(options),
            NullLogger<AspireResourceMcpTools>.Instance);
    }
}
