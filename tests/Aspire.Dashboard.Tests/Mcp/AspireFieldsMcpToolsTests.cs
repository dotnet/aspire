// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Tests.Shared;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Mcp;

public class AspireFieldsMcpToolsTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ListTelemetryFields_NoData_ReturnsEmptyCustomAttributes()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TELEMETRY FIELDS", result);
        Assert.Contains("known_fields", result);
        Assert.Contains("custom_attributes", result);
        Assert.Contains("traces", result);
        Assert.Contains("logs", result);
    }

    [Fact]
    public void ListTelemetryFields_WithTraces_ReturnsTraceFields()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1", traceAttributes: [KeyValuePair.Create("custom.trace.attr", "value1")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("traces", result);
        Assert.Contains("custom.trace.attr", result);
    }

    [Fact]
    public void ListTelemetryFields_WithLogs_ReturnsLogFields()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1", logAttributes: [KeyValuePair.Create("custom.log.attr", "value1")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("logs", result);
        Assert.Contains("custom.log.attr", result);
    }

    [Fact]
    public void ListTelemetryFields_TypeTraces_ReturnsOnlyTraceFields()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("custom.trace.attr", "value1")],
            logAttributes: [KeyValuePair.Create("custom.log.attr", "value2")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields(type: "traces");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("traces", result);
        Assert.DoesNotContain("\"logs\"", result);
        Assert.Contains("custom.trace.attr", result);
        Assert.DoesNotContain("custom.log.attr", result);
    }

    [Fact]
    public void ListTelemetryFields_TypeLogs_ReturnsOnlyLogFields()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("custom.trace.attr", "value1")],
            logAttributes: [KeyValuePair.Create("custom.log.attr", "value2")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields(type: "logs");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("logs", result);
        Assert.DoesNotContain("\"traces\"", result);
        Assert.Contains("custom.log.attr", result);
        Assert.DoesNotContain("custom.trace.attr", result);
    }

    [Fact]
    public void ListTelemetryFields_WithResource_FiltersToResource()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("app1.attr", "value1")]);
        AddResourceWithCustomAttributes(repository, "app2",
            traceAttributes: [KeyValuePair.Create("app2.attr", "value2")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("app1.attr", result);
        Assert.DoesNotContain("app2.attr", result);
    }

    [Fact]
    public void ListTelemetryFields_IncludesKnownFields()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields();

        // Assert
        Assert.NotNull(result);
        // Check for known trace fields
        Assert.Contains(KnownTraceFields.NameField, result);
        Assert.Contains(KnownTraceFields.StatusField, result);
        // Check for known log fields
        Assert.Contains(KnownStructuredLogFields.MessageField, result);
        Assert.Contains(KnownStructuredLogFields.CategoryField, result);
    }

    [Fact]
    public void ListTelemetryFields_InvalidType_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields(type: "invalid");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid type", result);
        Assert.Contains("'traces' or 'logs'", result);
    }

    [Fact]
    public void ListTelemetryFields_ResourceNotFound_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListTelemetryFields(resourceName: "nonexistent");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_ValidField_ReturnsValues()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("http.method", "GET")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "http.method");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TELEMETRY FIELD VALUES", result);
        Assert.Contains("http.method", result);
        Assert.Contains("GET", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_FieldWithMultipleValues_ReturnsAllWithCounts()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("http.method", "GET")]);
        AddResourceWithCustomAttributes(repository, "app2",
            traceAttributes: [KeyValuePair.Create("http.method", "POST")]);
        AddResourceWithCustomAttributes(repository, "app3",
            traceAttributes: [KeyValuePair.Create("http.method", "GET")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "http.method");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("GET", result);
        Assert.Contains("POST", result);
        Assert.Contains("count", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_UnknownField_ReturnsEmptyList()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "nonexistent.field");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# TELEMETRY FIELD VALUES", result);
        Assert.Contains("\"total_values\": 0", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_TypeTraces_QueriesTraceFields()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("custom.attr", "trace_value")],
            logAttributes: [KeyValuePair.Create("custom.attr", "log_value")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "custom.attr", type: "traces");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("traces", result);
        Assert.DoesNotContain("\"logs\"", result);
        Assert.Contains("trace_value", result);
        Assert.DoesNotContain("log_value", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_TypeLogs_QueriesLogFields()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("custom.attr", "trace_value")],
            logAttributes: [KeyValuePair.Create("custom.attr", "log_value")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "custom.attr", type: "logs");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("logs", result);
        Assert.DoesNotContain("\"traces\"", result);
        Assert.Contains("log_value", result);
        Assert.DoesNotContain("trace_value", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_WithResource_ValidatesResourceExists()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithCustomAttributes(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "http.method", resourceName: "nonexistent");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_OrderedByCountDescending()
    {
        // Arrange
        var repository = CreateRepository();
        // Add more GET requests to ensure it appears first in the order
        AddResourceWithCustomAttributes(repository, "app1",
            traceAttributes: [KeyValuePair.Create("http.method", "GET")]);
        AddResourceWithCustomAttributes(repository, "app2",
            traceAttributes: [KeyValuePair.Create("http.method", "GET")]);
        AddResourceWithCustomAttributes(repository, "app3",
            traceAttributes: [KeyValuePair.Create("http.method", "POST")]);
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "http.method", type: "traces");

        // Assert
        Assert.NotNull(result);
        // GET should appear before POST since it has more occurrences
        var getIndex = result.IndexOf("GET", StringComparison.Ordinal);
        var postIndex = result.IndexOf("POST", StringComparison.Ordinal);
        Assert.True(getIndex < postIndex, "GET should appear before POST (higher count first)");
    }

    [Fact]
    public void GetTelemetryFieldValues_MissingFieldName_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fieldName parameter is required", result);
    }

    [Fact]
    public void GetTelemetryFieldValues_InvalidType_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetTelemetryFieldValues(fieldName: "http.method", type: "invalid");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid type", result);
        Assert.Contains("'traces' or 'logs'", result);
    }

    private static AspireFieldsMcpTools CreateTools(TelemetryRepository repository, IDashboardClient? dashboardClient = null)
    {
        return new AspireFieldsMcpTools(
            repository,
            dashboardClient ?? new TestDashboardClient(),
            NullLogger<AspireFieldsMcpTools>.Instance);
    }

    private static void AddResourceWithCustomAttributes(
        TelemetryRepository repository,
        string name,
        string? instanceId = null,
        IEnumerable<KeyValuePair<string, string>>? traceAttributes = null,
        IEnumerable<KeyValuePair<string, string>>? logAttributes = null)
    {
        var idPrefix = instanceId != null ? $"{name}-{instanceId}" : name;

        var addContext = new AddContext();

        // Add traces with custom attributes
        var spanAttributes = traceAttributes?.ToList() ?? [];
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
                            CreateSpan(traceId: idPrefix + "1", spanId: idPrefix + "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: spanAttributes)
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);

        // Add logs with custom attributes
        var recordAttributes = logAttributes?.ToList() ?? [new KeyValuePair<string, string>("{OriginalFormat}", "Test {Log}"), new KeyValuePair<string, string>("Log", "Value!")];
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
                            CreateLogRecord(time: s_testTime, message: "Log entry!", attributes: recordAttributes)
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }
}
