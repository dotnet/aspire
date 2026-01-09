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
using OpenTelemetry.Proto.Common.V1;
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

    private static void AddResourceWithLogs(TelemetryRepository repository, string name, params (string message, SeverityNumber severity)[] logs)
    {
        var addContext = new AddContext();
        var logRecords = logs.Select((l, i) => CreateLogRecord(
            time: s_testTime.AddSeconds(i),
            message: l.message,
            severity: l.severity)).ToList();

        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: name),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope(),
                        LogRecords = { logRecords }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    private static void AddResourceWithLogAttributes(TelemetryRepository repository, string name, string message, params KeyValuePair<string, string>[] attributes)
    {
        var addContext = new AddContext();
        var allAttributes = new List<KeyValuePair<string, string>>(attributes)
        {
            new("{OriginalFormat}", "Test {Log}"),
            new("Log", "Value!")
        };

        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: name),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope(),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime, message: message, attributes: allAttributes)
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    [Fact]
    public void ListStructuredLogs_WithSeverityWarning_ReturnsWarningAndAbove()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithLogs(repository, "app1",
            ("Debug message", SeverityNumber.Debug),
            ("Info message", SeverityNumber.Info),
            ("Warning message", SeverityNumber.Warn),
            ("Error message", SeverityNumber.Error));
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: null,
            severity: "Warning");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.DoesNotContain("Debug message", result);
        Assert.DoesNotContain("Info message", result);
        Assert.Contains("Warning message", result);
        Assert.Contains("Error message", result);
    }

    [Fact]
    public void ListStructuredLogs_WithSeverityError_ReturnsOnlyErrorAndCritical()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithLogs(repository, "app1",
            ("Info message", SeverityNumber.Info),
            ("Warning message", SeverityNumber.Warn),
            ("Error message", SeverityNumber.Error),
            ("Critical message", SeverityNumber.Fatal));
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: null,
            severity: "Error");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.DoesNotContain("Info message", result);
        Assert.DoesNotContain("Warning message", result);
        Assert.Contains("Error message", result);
        Assert.Contains("Critical message", result);
    }

    [Fact]
    public void ListStructuredLogs_WithCategoryFilter_FiltersByCategory()
    {
        // Arrange
        var repository = CreateRepository();
        // We add logs via scope which becomes the category
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = new InstrumentationScope { Name = "MyApp.Controllers" },
                        LogRecords = { CreateLogRecord(time: s_testTime, message: "Controller log") }
                    }
                }
            }
        });
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = new InstrumentationScope { Name = "Microsoft.AspNetCore" },
                        LogRecords = { CreateLogRecord(time: s_testTime.AddSeconds(1), message: "AspNetCore log") }
                    }
                }
            }
        });

        var tools = CreateTools(repository);

        // Act - Filter for category containing "MyApp"
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: """[{"field":"log.category","condition":"contains","value":"MyApp"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.Contains("Controller log", result);
        Assert.DoesNotContain("AspNetCore log", result);
    }

    [Fact]
    public void ListStructuredLogs_WithMessageContainsFilter_FiltersMessages()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithLogs(repository, "app1",
            ("User logged in successfully", SeverityNumber.Info),
            ("Database connection established", SeverityNumber.Info),
            ("User logged out", SeverityNumber.Info));
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: """[{"field":"log.message","condition":"contains","value":"logged"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.Contains("User logged in successfully", result);
        Assert.Contains("User logged out", result);
        Assert.DoesNotContain("Database connection established", result);
    }

    [Fact]
    public void ListStructuredLogs_WithCustomAttributeFilter_FiltersAttribute()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithLogAttributes(repository, "app1", "Request from user1",
            new KeyValuePair<string, string>("user.id", "user1"));
        AddResourceWithLogAttributes(repository, "app2", "Request from user2",
            new KeyValuePair<string, string>("user.id", "user2"));
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: """[{"field":"user.id","condition":"equals","value":"user1"}]""");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.Contains("Request from user1", result);
        Assert.DoesNotContain("Request from user2", result);
    }

    [Fact]
    public void ListStructuredLogs_WithMultipleFilters_AppliesAndLogic()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithLogs(repository, "app1",
            ("Info from app1", SeverityNumber.Info),
            ("Warning from app1", SeverityNumber.Warn));
        AddResourceWithLogs(repository, "app2",
            ("Info from app2", SeverityNumber.Info),
            ("Warning from app2", SeverityNumber.Warn));
        var tools = CreateTools(repository);

        // Act - Filter for Warning level AND resource app1
        var result = tools.ListStructuredLogs(
            resourceName: "app1",
            filters: null,
            severity: "Warning");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.Contains("Warning from app1", result);
        Assert.DoesNotContain("Info from app1", result);
        Assert.DoesNotContain("app2", result);
    }

    [Fact]
    public void ListStructuredLogs_WithInvalidSeverity_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: null,
            severity: "NotAValidSeverity");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid severity", result);
    }

    [Fact]
    public void ListStructuredLogs_FiltersAndResource_CombinesCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithLogs(repository, "app1",
            ("Error in app1", SeverityNumber.Error));
        AddResourceWithLogs(repository, "app2",
            ("Error in app2", SeverityNumber.Error));
        var tools = CreateTools(repository);

        // Act - Filter by both resource and severity
        var result = tools.ListStructuredLogs(
            resourceName: "app1",
            filters: null,
            severity: "Error");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# STRUCTURED LOGS DATA", result);
        Assert.Contains("Error in app1", result);
        Assert.DoesNotContain("Error in app2", result);
    }

    [Fact]
    public void ListStructuredLogs_WithInvalidFilterJson_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListStructuredLogs(
            resourceName: null,
            filters: "not valid json");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid filters JSON", result);
    }

    [Fact]
    public void GetTrace_WithValidTraceId_ReturnsTraceData()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // The trace ID is "app11" (resource name + "1") encoded as hex
        var hexTraceId = GetHexId("app11");

        // Act
        var result = tools.GetTrace(traceId: hexTraceId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACE DATA", result);
        Assert.Contains("app1", result);
    }

    [Fact]
    public void GetTrace_WithInvalidTraceId_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTrace(traceId: "nonexistent-trace");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("not found", result);
    }

    [Fact]
    public void GetTrace_WithMissingTraceId_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTrace(traceId: null!);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("traceId is required", result);
    }

    [Fact]
    public void GetTrace_WithEmptyTraceId_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTrace(traceId: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("traceId is required", result);
    }

    [Fact]
    public void GetTrace_IncludesDashboardLink()
    {
        // Arrange
        var repository = CreateRepository();
        AddResource(repository, "app1");
        var tools = CreateTools(repository);

        // The trace ID is "app11" (resource name + "1") encoded as hex
        var hexTraceId = GetHexId("app11");

        // Act
        var result = tools.GetTrace(traceId: hexTraceId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TRACE DATA", result);
        // Dashboard link should be included
        Assert.Contains("dashboard_link", result);
    }
}
