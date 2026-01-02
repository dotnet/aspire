// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Mcp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Tests.Shared;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Mcp;

public class AspireMetricsMcpToolsTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ListMetrics_NoResource_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListMetrics(resourceName: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("resourceName parameter is required", result);
    }

    [Fact]
    public void ListMetrics_ResourceNotFound_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListMetrics(resourceName: "nonexistent");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void ListMetrics_WithResource_ReturnsInstruments()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListMetrics(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# METRICS FOR APP1", result);
        Assert.Contains("TestMeter", result);
        Assert.Contains("test.metric", result);
        Assert.Contains("total_instruments", result);
    }

    [Fact]
    public void ListMetrics_MultipleMeters_GroupsByMeter()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "Meter1", metricName: "metric1");
        AddResourceWithMetrics(repository, "app1", meterName: "Meter2", metricName: "metric2");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListMetrics(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Meter1", result);
        Assert.Contains("Meter2", result);
        Assert.Contains("metric1", result);
        Assert.Contains("metric2", result);
    }

    [Fact]
    public void ListMetrics_IncludesInstrumentMetadata()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListMetrics(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("name", result);
        Assert.Contains("description", result);
        Assert.Contains("unit", result);
        Assert.Contains("type", result);
    }

    [Fact]
    public void ListMetrics_NoMetrics_ReturnsNoMetricsMessage()
    {
        // Arrange
        var repository = CreateRepository();
        // Add a resource with traces but no metrics
        AddResourceWithTracesOnly(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.ListMetrics(resourceName: "app1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("No metrics found", result);
    }

    [Fact]
    public void GetMetricData_ValidInstrument_ReturnsData()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "test.metric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# METRIC DATA: test.metric", result);
        Assert.Contains("dimensions", result);
        Assert.Contains("time_window", result);
    }

    [Fact]
    public void GetMetricData_InvalidInstrument_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "nonexistent");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("not found", result);
        Assert.Contains("nonexistent", result);
    }

    [Fact]
    public void GetMetricData_InvalidMeter_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "NonexistentMeter",
            instrumentName: "test.metric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("not found", result);
    }

    [Fact]
    public void GetMetricData_ResourceNotFound_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "nonexistent",
            meterName: "TestMeter",
            instrumentName: "test.metric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("doesn't have any telemetry", result);
    }

    [Fact]
    public void GetMetricData_DefaultDuration_Uses5Minutes()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "test.metric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("5m", result);
    }

    [Fact]
    public void GetMetricData_CustomDuration_UsesSpecifiedDuration()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "test.metric",
            duration: "1h");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("1h", result);
    }

    [Fact]
    public void GetMetricData_InvalidDuration_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "test.metric",
            duration: "invalid");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid duration", result);
        Assert.Contains("Supported values", result);
    }

    [Fact]
    public void GetMetricData_MissingResourceName_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "",
            meterName: "TestMeter",
            instrumentName: "test.metric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("resourceName parameter is required", result);
    }

    [Fact]
    public void GetMetricData_MissingMeterName_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "",
            instrumentName: "test.metric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("meterName parameter is required", result);
    }

    [Fact]
    public void GetMetricData_MissingInstrumentName_ReturnsError()
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("instrumentName parameter is required", result);
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("15m")]
    [InlineData("30m")]
    [InlineData("1h")]
    [InlineData("3h")]
    [InlineData("6h")]
    [InlineData("12h")]
    public void GetMetricData_AllSupportedDurations_Succeed(string duration)
    {
        // Arrange
        var repository = CreateRepository();
        AddResourceWithMetrics(repository, "app1", meterName: "TestMeter", metricName: "test.metric");
        var tools = CreateTools(repository);

        // Act
        var result = tools.GetMetricData(
            resourceName: "app1",
            meterName: "TestMeter",
            instrumentName: "test.metric",
            duration: duration);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("# METRIC DATA", result);
        Assert.Contains(duration, result);
    }

    private static AspireMetricsMcpTools CreateTools(TelemetryRepository repository, IDashboardClient? dashboardClient = null)
    {
        var dashboardOptions = new DashboardOptions
        {
            Frontend = new FrontendOptions { BrowserToken = "test-token" }
        };
        return new AspireMetricsMcpTools(
            repository,
            dashboardClient ?? new TestDashboardClient(),
            new TestOptionsMonitor<DashboardOptions>(dashboardOptions),
            NullLogger<AspireMetricsMcpTools>.Instance);
    }

    private static void AddResourceWithMetrics(
        TelemetryRepository repository,
        string name,
        string? instanceId = null,
        string? meterName = null,
        string? metricName = null)
    {
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: name, instanceId: instanceId),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: meterName ?? "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: metricName ?? "test-metric", startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    private static void AddResourceWithTracesOnly(
        TelemetryRepository repository,
        string name,
        string? instanceId = null)
    {
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<OpenTelemetry.Proto.Trace.V1.ResourceSpans>()
        {
            new OpenTelemetry.Proto.Trace.V1.ResourceSpans
            {
                Resource = CreateResource(name: name, instanceId: instanceId),
                ScopeSpans =
                {
                    new OpenTelemetry.Proto.Trace.V1.ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: name + "1", spanId: name + "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        Assert.Equal(0, addContext.FailureCount);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
