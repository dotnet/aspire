// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.Model;

public sealed class TelemetryExportServiceTests
{
    private static readonly DateTime s_testTime = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertLogsToOtlpJson_SingleLog_ReturnsCorrectStructure()
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "TestService", instanceId: "instance-1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime, message: "Test log message", severity: OpenTelemetry.Proto.Logs.V1.SeverityNumber.Info, eventName: "TestEvent", traceId: "abcd1234abcd1234", spanId: "efgh5678", attributes: [new KeyValuePair<string, string>("custom.attr", "custom-value")]) }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var logs = repository.GetLogs(new GetLogsContext
        {
            ResourceKey = resource.ResourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = []
        });

        // Act
        var result = TelemetryExportService.ConvertLogsToOtlpJson(resource, logs.Items);

        // Assert
        Assert.NotNull(result.ResourceLogs);
        Assert.Single(result.ResourceLogs);

        var resourceLogs = result.ResourceLogs[0];
        Assert.NotNull(resourceLogs.Resource);
        Assert.NotNull(resourceLogs.Resource.Attributes);
        Assert.Contains(resourceLogs.Resource.Attributes, a => a.Key == OtlpResource.SERVICE_NAME && a.Value?.StringValue == "TestService");
        Assert.Contains(resourceLogs.Resource.Attributes, a => a.Key == OtlpResource.SERVICE_INSTANCE_ID && a.Value?.StringValue == "instance-1");

        Assert.NotNull(resourceLogs.ScopeLogs);
        Assert.Single(resourceLogs.ScopeLogs);

        var scopeLogs = resourceLogs.ScopeLogs[0];
        Assert.NotNull(scopeLogs.Scope);
        Assert.Equal("TestLogger", scopeLogs.Scope.Name);

        Assert.NotNull(scopeLogs.LogRecords);
        Assert.Single(scopeLogs.LogRecords);

        var logRecord = scopeLogs.LogRecords[0];
        Assert.Equal("Test log message", logRecord.Body?.StringValue);
        Assert.Equal((int)SeverityNumber.Info, logRecord.SeverityNumber);
        Assert.Equal("Information", logRecord.SeverityText);
        Assert.Equal("TestEvent", logRecord.EventName);
        Assert.Equal(OtlpHelpers.DateTimeToUnixNanoseconds(s_testTime), logRecord.TimeUnixNano);
        Assert.Equal("61626364313233346162636431323334", logRecord.TraceId); // hex of UTF-8 bytes of "abcd1234abcd1234"
        Assert.Equal("6566676835363738", logRecord.SpanId); // hex of UTF-8 bytes of "efgh5678"
        Assert.NotNull(logRecord.Attributes);
        Assert.Contains(logRecord.Attributes, a => a.Key == "custom.attr" && a.Value?.StringValue == "custom-value");
    }

    [Fact]
    public void ConvertLogsToOtlpJson_MultipleLogs_GroupsByScope()
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "TestService", instanceId: "instance-1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("Logger1"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime, message: "Log from Logger1"),
                            CreateLogRecord(time: s_testTime.AddSeconds(1), message: "Another log from Logger1")
                        }
                    },
                    new ScopeLogs
                    {
                        Scope = CreateScope("Logger2"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddSeconds(2), message: "Log from Logger2") }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var logs = repository.GetLogs(new GetLogsContext
        {
            ResourceKey = resource.ResourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = []
        });

        // Act
        var result = TelemetryExportService.ConvertLogsToOtlpJson(resource, logs.Items);

        // Assert
        Assert.NotNull(result.ResourceLogs);
        Assert.Single(result.ResourceLogs);

        var resourceLogs = result.ResourceLogs[0];
        Assert.NotNull(resourceLogs.ScopeLogs);
        Assert.Equal(2, resourceLogs.ScopeLogs.Length);

        var logger1Scope = resourceLogs.ScopeLogs.FirstOrDefault(s => s.Scope?.Name == "Logger1");
        Assert.NotNull(logger1Scope);
        Assert.NotNull(logger1Scope.LogRecords);
        Assert.Equal(2, logger1Scope.LogRecords.Length);

        var logger2Scope = resourceLogs.ScopeLogs.FirstOrDefault(s => s.Scope?.Name == "Logger2");
        Assert.NotNull(logger2Scope);
        Assert.NotNull(logger2Scope.LogRecords);
        Assert.Single(logger2Scope.LogRecords);
    }

    [Theory]
    [InlineData(SeverityNumber.Trace, "Trace")]
    [InlineData(SeverityNumber.Trace2, "Trace")]
    [InlineData(SeverityNumber.Trace3, "Trace")]
    [InlineData(SeverityNumber.Trace4, "Trace")]
    [InlineData(SeverityNumber.Debug, "Debug")]
    [InlineData(SeverityNumber.Debug2, "Debug")]
    [InlineData(SeverityNumber.Debug3, "Debug")]
    [InlineData(SeverityNumber.Debug4, "Debug")]
    [InlineData(SeverityNumber.Info, "Information")]
    [InlineData(SeverityNumber.Info2, "Information")]
    [InlineData(SeverityNumber.Info3, "Information")]
    [InlineData(SeverityNumber.Info4, "Information")]
    [InlineData(SeverityNumber.Warn, "Warning")]
    [InlineData(SeverityNumber.Warn2, "Warning")]
    [InlineData(SeverityNumber.Warn3, "Warning")]
    [InlineData(SeverityNumber.Warn4, "Warning")]
    [InlineData(SeverityNumber.Error, "Error")]
    [InlineData(SeverityNumber.Error2, "Error")]
    [InlineData(SeverityNumber.Error3, "Error")]
    [InlineData(SeverityNumber.Error4, "Error")]
    [InlineData(SeverityNumber.Fatal, "Critical")]
    [InlineData(SeverityNumber.Fatal2, "Critical")]
    [InlineData(SeverityNumber.Fatal3, "Critical")]
    [InlineData(SeverityNumber.Fatal4, "Critical")]
    public void ConvertLogsToOtlpJson_RoundTripsSeverityNumber(SeverityNumber inputSeverity, string expectedSeverityText)
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope(),
                        LogRecords = { CreateLogRecord(severity: inputSeverity) }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var logs = repository.GetLogs(new GetLogsContext
        {
            ResourceKey = resource.ResourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = []
        });

        // Act
        var result = TelemetryExportService.ConvertLogsToOtlpJson(resource, logs.Items);

        // Assert
        var logRecord = result.ResourceLogs![0].ScopeLogs![0].LogRecords![0];
        // Verify exact severity number is preserved (round-trip)
        Assert.Equal((int)inputSeverity, logRecord.SeverityNumber);
        // Verify severity text is the mapped LogLevel
        Assert.Equal(expectedSeverityText, logRecord.SeverityText);
    }

    [Fact]
    public void ConvertTracesToOtlpJson_SingleTrace_ReturnsCorrectStructure()
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "TestService", instanceId: "instance-1"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope("TestTracer"),
                        Spans =
                        {
                            CreateSpan(
                                traceId: "trace123456789012",
                                spanId: "span1234",
                                startTime: s_testTime,
                                endTime: s_testTime.AddSeconds(5),
                                kind: Span.Types.SpanKind.Server,
                                status: new Status { Code = Status.Types.StatusCode.Error, Message = "Something went wrong" },
                                attributes: [new KeyValuePair<string, string>("http.method", "GET")])
                        }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resource.ResourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            FilterText = string.Empty,
            Filters = []
        });

        // Act
        var result = TelemetryExportService.ConvertTracesToOtlpJson(resource, traces.PagedResult.Items);

        // Assert
        Assert.NotNull(result.ResourceSpans);
        Assert.Single(result.ResourceSpans);

        var resourceSpans = result.ResourceSpans[0];
        Assert.NotNull(resourceSpans.Resource);
        Assert.NotNull(resourceSpans.Resource.Attributes);
        Assert.Contains(resourceSpans.Resource.Attributes, a => a.Key == OtlpResource.SERVICE_NAME && a.Value?.StringValue == "TestService");

        Assert.NotNull(resourceSpans.ScopeSpans);
        Assert.Single(resourceSpans.ScopeSpans);

        var scopeSpans = resourceSpans.ScopeSpans[0];
        Assert.NotNull(scopeSpans.Scope);
        Assert.Equal("TestTracer", scopeSpans.Scope.Name);

        Assert.NotNull(scopeSpans.Spans);
        Assert.Single(scopeSpans.Spans);

        var span = scopeSpans.Spans[0];
        Assert.Equal((int)OtlpSpanKind.Server, span.Kind);
        Assert.Equal("7472616365313233343536373839303132", span.TraceId); // hex of UTF-8 bytes of "trace123456789012"
        Assert.Equal("7370616e31323334", span.SpanId); // hex of UTF-8 bytes of "span1234"
        Assert.Equal("Test span. Id: span1234", span.Name);
        Assert.Equal(OtlpHelpers.DateTimeToUnixNanoseconds(s_testTime), span.StartTimeUnixNano);
        Assert.Equal(OtlpHelpers.DateTimeToUnixNanoseconds(s_testTime.AddSeconds(5)), span.EndTimeUnixNano);
        Assert.NotNull(span.Status);
        Assert.Equal((int)Status.Types.StatusCode.Error, span.Status.Code);
        Assert.Equal("Something went wrong", span.Status.Message);
        Assert.NotNull(span.Attributes);
        Assert.Contains(span.Attributes, a => a.Key == "http.method" && a.Value?.StringValue == "GET");
    }

    [Fact]
    public void ConvertTracesToOtlpJson_SpanWithParent_IncludesParentSpanId()
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "trace123456789012", spanId: "parent12", startTime: s_testTime, endTime: s_testTime.AddSeconds(10)),
                            CreateSpan(traceId: "trace123456789012", spanId: "child123", startTime: s_testTime.AddSeconds(1), endTime: s_testTime.AddSeconds(5), parentSpanId: "parent12")
                        }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resource.ResourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            FilterText = string.Empty,
            Filters = []
        });

        // Act
        var result = TelemetryExportService.ConvertTracesToOtlpJson(resource, traces.PagedResult.Items);

        // Assert
        var spans = result.ResourceSpans![0].ScopeSpans![0].Spans!;
        Assert.Equal(2, spans.Length);

        var parentSpan = spans.First(s => s.ParentSpanId is null);
        var childSpan = spans.First(s => s.ParentSpanId is not null);

        Assert.NotNull(childSpan.ParentSpanId);
    }

    [Fact]
    public void ConvertMetricsToOtlpJson_SingleInstrument_ReturnsCorrectStructure()
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<OpenTelemetry.Proto.Metrics.V1.ResourceMetrics>()
        {
            new OpenTelemetry.Proto.Metrics.V1.ResourceMetrics
            {
                Resource = CreateResource(name: "TestService", instanceId: "instance-1"),
                ScopeMetrics =
                {
                    new OpenTelemetry.Proto.Metrics.V1.ScopeMetrics
                    {
                        Scope = CreateScope("TestMeter"),
                        Metrics = { CreateSumMetric("test_counter", s_testTime) }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var instruments = repository.GetInstrumentsSummaries(resource.ResourceKey);

        // Act
        var result = TelemetryExportService.ConvertMetricsToOtlpJson(resource, instruments);

        // Assert
        Assert.NotNull(result.ResourceMetrics);
        Assert.Single(result.ResourceMetrics);

        var resourceMetrics = result.ResourceMetrics[0];
        Assert.NotNull(resourceMetrics.Resource);
        Assert.NotNull(resourceMetrics.Resource.Attributes);
        Assert.Contains(resourceMetrics.Resource.Attributes, a => a.Key == OtlpResource.SERVICE_NAME && a.Value?.StringValue == "TestService");

        Assert.NotNull(resourceMetrics.ScopeMetrics);
        Assert.Single(resourceMetrics.ScopeMetrics);

        var scopeMetrics = resourceMetrics.ScopeMetrics[0];
        Assert.NotNull(scopeMetrics.Scope);
        Assert.Equal("TestMeter", scopeMetrics.Scope.Name);

        Assert.NotNull(scopeMetrics.Metrics);
        Assert.Single(scopeMetrics.Metrics);

        var metric = scopeMetrics.Metrics[0];
        Assert.Equal("test_counter", metric.Name);
        Assert.Equal("Test metric description", metric.Description);
        Assert.Equal("widget", metric.Unit);
    }

    [Fact]
    public void ConvertMetricsToOtlpJson_MultipleInstruments_GroupsByScope()
    {
        // Arrange
        var repository = CreateRepository();
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<OpenTelemetry.Proto.Metrics.V1.ResourceMetrics>()
        {
            new OpenTelemetry.Proto.Metrics.V1.ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new OpenTelemetry.Proto.Metrics.V1.ScopeMetrics
                    {
                        Scope = CreateScope("Meter1"),
                        Metrics =
                        {
                            CreateSumMetric("counter1", s_testTime),
                            CreateSumMetric("counter2", s_testTime)
                        }
                    },
                    new OpenTelemetry.Proto.Metrics.V1.ScopeMetrics
                    {
                        Scope = CreateScope("Meter2"),
                        Metrics = { CreateHistogramMetric("histogram1", s_testTime) }
                    }
                }
            }
        });

        var resources = repository.GetResources();
        var resource = resources[0];
        var instruments = repository.GetInstrumentsSummaries(resource.ResourceKey);

        // Act
        var result = TelemetryExportService.ConvertMetricsToOtlpJson(resource, instruments);

        // Assert
        Assert.NotNull(result.ResourceMetrics);
        Assert.Single(result.ResourceMetrics);

        var resourceMetrics = result.ResourceMetrics[0];
        Assert.NotNull(resourceMetrics.ScopeMetrics);
        Assert.Equal(2, resourceMetrics.ScopeMetrics.Length);

        var meter1Scope = resourceMetrics.ScopeMetrics.FirstOrDefault(s => s.Scope?.Name == "Meter1");
        Assert.NotNull(meter1Scope);
        Assert.NotNull(meter1Scope.Metrics);
        Assert.Equal(2, meter1Scope.Metrics.Length);

        var meter2Scope = resourceMetrics.ScopeMetrics.FirstOrDefault(s => s.Scope?.Name == "Meter2");
        Assert.NotNull(meter2Scope);
        Assert.NotNull(meter2Scope.Metrics);
        Assert.Single(meter2Scope.Metrics);
    }
}
