// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Tests.Model;
using Aspire.Dashboard.Tests.Shared;
using Aspire.Tests.Shared.DashboardModel;
using Aspire.Tests.Shared.Telemetry;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Mcp;

public class AspireTelemetryMcpToolsTests
{
    private static readonly ResourcePropertyViewModel s_excludeFromMcpProperty = new ResourcePropertyViewModel(KnownProperties.Resource.ExcludeFromMcp, Value.ForBool(true), isValueSensitive: false, knownProperty: null, priority: 0);
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
        Assert.Contains("app1", result);
    }

    [Fact]
    public void ListTraces_ResourceOptOut_FilterTraces()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1", "instance1");
        AddResource(repository, "app2", "instance1");

        var resource = ModelTestHelpers.CreateResource(
            resourceName: "app1-instance1",
            displayName: "app1",
            properties: new Dictionary<string, ResourcePropertyViewModel> { [KnownProperties.Resource.ExcludeFromMcp] = s_excludeFromMcpProperty });
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);

        var tools = CreateTools(repository, dashboardClient);

        // Act
        var result = tools.ListTraces();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
        Assert.DoesNotContain("app1", result);
        Assert.Contains("app2", result);
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
    public void ListStructuredLogs_HasResource_ReturnsLogs()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.Contains("app1", result);
    }

    [Fact]
    public void ListStructuredLogs_ResourceOptOut_FiltersLogs()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1", "instance1");
        AddResource(repository, "app2", "instance1");

        var resource = ModelTestHelpers.CreateResource(
            resourceName: "app1-instance1",
            displayName: "app1",
            properties: new Dictionary<string, ResourcePropertyViewModel> { [KnownProperties.Resource.ExcludeFromMcp] = s_excludeFromMcpProperty });
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: [resource]);

        var tools = CreateTools(repository, dashboardClient);

        // Act
        var result = tools.ListStructuredLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.DoesNotContain("app1", result);
        Assert.Contains("app2", result);
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
        Assert.Contains("app1", result);
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

    [Fact]
    public void ListTraces_WithSingleFilter_ReturnsFilteredTraces()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithAttributes(repository, "app1", new KeyValuePair<string, string>("http.method", "POST"));
        AddResourceWithAttributes(repository, "app2", new KeyValuePair<string, string>("http.method", "GET"));
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraces(
            resourceName: null,
            filters: """[{"field":"http.method","condition":"equals","value":"POST"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
        // The filter should be applied - app1 has POST, app2 has GET
        Assert.Contains("app1", result);
        Assert.DoesNotContain("app2", result);
    }

    [Fact]
    public void ListTraces_WithMultipleFilters_AppliesAndLogic()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithAttributes(repository, "app1",
            new KeyValuePair<string, string>("http.method", "POST"),
            new KeyValuePair<string, string>("http.status_code", "200"));
        AddResourceWithAttributes(repository, "app2",
            new KeyValuePair<string, string>("http.method", "POST"),
            new KeyValuePair<string, string>("http.status_code", "500"));
        var tools = CreateTools(repository);

        // Act - Filter for POST with 200 status code
        var result = tools.ListTraces(
            resourceName: null,
            filters: """[{"field":"http.method","condition":"equals","value":"POST"},{"field":"http.status_code","condition":"equals","value":"200"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
        Assert.Contains("app1", result);
        Assert.DoesNotContain("app2", result);
    }

    [Fact]
    public void ListTraces_WithSearchText_FiltersSpanNames()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithSpanName(repository, "app1", "GET /api/users");
        AddResourceWithSpanName(repository, "app2", "POST /api/orders");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraces(
            resourceName: null,
            filters: null,
            searchText: "/api/users");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
        Assert.Contains("app1", result);
        Assert.DoesNotContain("app2", result);
    }

    [Fact]
    public void ListTraces_WithStatusErrorFilter_ReturnsOnlyErrors()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithStatus(repository, "app1", Status.Types.StatusCode.Error);
        AddResourceWithStatus(repository, "app2", Status.Types.StatusCode.Ok);
        var tools = CreateTools(repository);

        // Act - Using the correct field name trace.status
        var result = tools.ListTraces(
            resourceName: null,
            filters: """[{"field":"trace.status","condition":"equals","value":"Error"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
        Assert.Contains("app1", result);
        Assert.DoesNotContain("app2", result);
    }

    [Fact]
    public void ListTraces_WithInvalidFilterJson_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTraces(
            resourceName: null,
            filters: "not valid json");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid filters JSON", result);
    }

    [Fact]
    public void ListTraces_FiltersAndResource_CombinesCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithAttributes(repository, "app1", new KeyValuePair<string, string>("http.method", "POST"));
        AddResourceWithAttributes(repository, "app2", new KeyValuePair<string, string>("http.method", "POST"));
        var tools = CreateTools(repository);

        // Act - Filter by both resource and attribute
        var result = tools.ListTraces(
            resourceName: "app1",
            filters: """[{"field":"http.method","condition":"equals","value":"POST"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACES DATA", result);
        Assert.Contains("app1", result);
        // app2 should not appear because we filtered by resource name
        Assert.DoesNotContain("app2", result);
    }

    private static AspireTelemetryMcpTools CreateTools(TelemetryRepository repository, IDashboardClient? dashboardClient = null)
    {
        var options = new DashboardOptions();
        options.Frontend.EndpointUrls = "https://localhost:1234";
        options.Frontend.PublicUrl = "https://localhost:8080";
        Assert.True(options.Frontend.TryParseOptions(out _));

        return new AspireTelemetryMcpTools(
            repository,
            [],
            new TestOptionsMonitor<DashboardOptions>(options),
            dashboardClient ?? new TestDashboardClient(),
            NullLogger<AspireTelemetryMcpTools>.Instance);
    }

    private static TelemetryRepository CreateRepository()
    {
        return TelemetryTestHelpers.CreateRepository();
    }

    private static void AddResource(TelemetryRepository repository, string name, string? instanceId = null)
    {
        var idPrefix = instanceId != null ? $"{name}-{instanceId}" : name;

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: name, instanceId: instanceId),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: idPrefix + "1", spanId: idPrefix + "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: idPrefix + "1", spanId: idPrefix + "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: idPrefix + "1-1"),
                            CreateSpan(traceId: idPrefix + "2", spanId: idPrefix + "2-1", startTime: s_testTime.AddMinutes(6), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);

        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: name, instanceId: instanceId),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope(),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime, message: "Log entry!")
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    private static void AddResourceWithAttributes(TelemetryRepository repository, string name, params KeyValuePair<string, string>[] attributes)
    {
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: name),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: name + "1", spanId: name + "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes)
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    private static void AddResourceWithSpanName(TelemetryRepository repository, string name, string spanName)
    {
        var addContext = new AddContext();
        var span = CreateSpan(traceId: name + "1", spanId: name + "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10));
        span.Name = spanName;

        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: name),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { span }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    private static void AddResourceWithStatus(TelemetryRepository repository, string name, Status.Types.StatusCode statusCode)
    {
        var addContext = new AddContext();
        var span = CreateSpan(
            traceId: name + "1",
            spanId: name + "1-1",
            startTime: s_testTime.AddMinutes(1),
            endTime: s_testTime.AddMinutes(10),
            status: new Status { Code = statusCode });

        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: name),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { span }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }
}
