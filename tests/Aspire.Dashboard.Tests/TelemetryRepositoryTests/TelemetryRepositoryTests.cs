// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class TelemetryRepositoryTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AddData_WhilePaused_IsDiscarded()
    {
        // Arrange
        var pauseManager = new PauseManager();
        var repository = CreateRepository(pauseManager: pauseManager);
        using var subscription = repository.OnNewLogs(resourceKey: null, SubscriptionType.Other, () => Task.CompletedTask);

        // Act and assert
        pauseManager.SetStructuredLogsPaused(true);
        pauseManager.SetMetricsPaused(true);
        pauseManager.SetTracesPaused(true);
        AddLog();
        AddMetric();
        AddTrace();

        var resourceKey = new ResourceKey("resource", "resource");
        Assert.Empty(repository.GetLogs(new GetLogsContext { ResourceKey = resourceKey, Count = 100, Filters = [], StartIndex = 0 }).Items);
        Assert.Null(repository.GetResource(resourceKey));
        Assert.Empty(repository.GetTraces(new GetTracesRequest { ResourceKey = resourceKey, Count = 100, Filters = [], StartIndex = 0, FilterText = string.Empty }).PagedResult.Items);

        pauseManager.SetStructuredLogsPaused(false);
        pauseManager.SetMetricsPaused(false);
        pauseManager.SetTracesPaused(false);

        AddLog();
        AddMetric();
        AddTrace();
        Assert.Single(repository.GetLogs(new GetLogsContext { ResourceKey = resourceKey, Count = 100, Filters = [], StartIndex = 0 }).Items);
        var resource = repository.GetResource(resourceKey);
        Assert.NotNull(resource);
        Assert.NotEmpty(resource.GetInstrumentsSummary());
        Assert.Single(repository.GetTraces(new GetTracesRequest { ResourceKey = resourceKey, Count = 100, Filters = [], StartIndex = 0, FilterText = string.Empty }).PagedResult.Items);

        void AddLog()
        {
            var addContext = new AddContext();
            repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
            {
                new ResourceLogs
                {
                    Resource = CreateResource(name: "resource", instanceId: "resource"),
                    ScopeLogs =
                    {
                        new ScopeLogs
                        {
                            Scope = CreateScope("TestLogger"),
                            LogRecords =
                            {
                                CreateLogRecord(time: DateTime.Now, message: "1", severity: SeverityNumber.Error),
                            }
                        }
                    }
                }
            });
        }

        void AddMetric()
        {
            var addContext = new AddContext();
            repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
            {
                new ResourceMetrics
                {
                    Resource = CreateResource("resource", instanceId: "resource"),
                    ScopeMetrics =
                    {
                        new ScopeMetrics
                        {
                            Scope = CreateScope(name: "test-meter"),
                            Metrics =
                            {
                                CreateSumMetric(metricName: "test", startTime: DateTime.Now.AddMinutes(1)),
                                CreateSumMetric(metricName: "test", startTime: DateTime.Now.AddMinutes(2)),
                                CreateSumMetric(metricName: "test2", startTime: DateTime.Now.AddMinutes(1)),
                            }
                        },
                        new ScopeMetrics
                        {
                            Scope = CreateScope(name: "test-meter2"),
                            Metrics =
                            {
                                CreateSumMetric(metricName: "test", startTime: DateTime.Now.AddMinutes(1)),
                                CreateHistogramMetric(metricName: "test2", startTime: DateTime.Now.AddMinutes(1))
                            }
                        }
                    }
                }
            });
        }

        void AddTrace()
        {
            var addContext = new AddContext();
            repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
            {
                new ResourceSpans
                {
                    Resource = CreateResource("resource", instanceId: "resource"),
                    ScopeSpans =
                    {
                        new ScopeSpans
                        {
                            Scope = CreateScope(),
                            Spans =
                            {
                                CreateSpan(traceId: "1", spanId: "1-1", startTime: DateTime.Now.AddMinutes(1), endTime: DateTime.Now.AddMinutes(10)),
                                CreateSpan(traceId: "1", spanId: "1-2", startTime: DateTime.Now.AddMinutes(5), endTime: DateTime.Now.AddMinutes(10), parentSpanId: "1-1")
                            }
                        }
                    }
                }
            });
        }
    }

    [Fact]
    public void Subscription_MultipleDisposes_UnsubscribeOnce()
    {
        // Arrange
        var telemetryRepository = CreateRepository();
        var unsubscribeCallCount = 0;

        var subscription = new Subscription(
            name: "Test",
            resourceKey: null,
            subscriptionType: SubscriptionType.Read,
            callback: () => Task.CompletedTask,
            unsubscribe: () => unsubscribeCallCount++,
            executionContext: null,
            telemetryRepository: telemetryRepository);

        // Act
        subscription.Dispose();
        subscription.Dispose();

        // Assert
        Assert.Equal(1, unsubscribeCallCount);
    }

    [Fact]
    public async Task Subscription_ExecuteAfterDispose_LogWithNoExecute()
    {
        // Arrange
        var tcs = new TaskCompletionSource<WriteContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var testSink = new TestSink();
        testSink.MessageLogged += (write) =>
        {
            if (write.Message == "Callback 'Test' has been disposed.")
            {
                tcs.TrySetResult(write);
            }
        };
        var factory = LoggerFactory.Create(b =>
        {
            b.AddProvider(new TestLoggerProvider(testSink));
            b.SetMinimumLevel(LogLevel.Trace);
        });

        var telemetryRepository = CreateRepository(loggerFactory: factory);

        var subscription = new Subscription(
            name: "Test",
            resourceKey: null,
            subscriptionType: SubscriptionType.Read,
            callback: () => Task.CompletedTask,
            unsubscribe: () => { },
            executionContext: null,
            telemetryRepository: telemetryRepository);

        subscription.Dispose();

        // Act
        subscription.Execute();

        // Assert
        await tcs.Task.DefaultTimeout();
    }

    [Fact]
    public void ClearSelectedSignals_ClearsSelectedDataTypes_ForSpecificResources()
    {
        // Arrange
        var repository = CreateRepository();

        AddTestData(repository, "resource1", "123");
        AddTestData(repository, "resource2", "456");

        // Verify unviewed error logs exist before clearing
        var unviewedBefore = repository.GetResourceUnviewedErrorLogsCount();
        Assert.True(unviewedBefore.TryGetValue(new ResourceKey("resource1", "123"), out var errorCount1));
        Assert.Equal(1, errorCount1);
        Assert.True(unviewedBefore.TryGetValue(new ResourceKey("resource2", "456"), out var errorCount2));
        Assert.Equal(1, errorCount2);

        // Act - Clear only structured logs for resource1
        var selectedResources = new Dictionary<string, HashSet<AspireDataType>>
        {
            ["resource1-123"] = [AspireDataType.StructuredLogs]
        };
        repository.ClearSelectedSignals(selectedResources);

        // Assert - resource1 unviewed error logs cleared
        var unviewedAfter = repository.GetResourceUnviewedErrorLogsCount();
        Assert.False(unviewedAfter.TryGetValue(new ResourceKey("resource1", "123"), out _));
        Assert.True(unviewedAfter.TryGetValue(new ResourceKey("resource2", "456"), out errorCount2));
        Assert.Equal(1, errorCount2);

        // Assert - resource1 logs cleared, but traces and metrics remain
        var logs = repository.GetLogs(new GetLogsContext { ResourceKey = null, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Single(logs.Items);
        Assert.Equal("log-resource2-456", logs.Items[0].Message);

        var traces = repository.GetTraces(new GetTracesRequest { ResourceKey = null, FilterText = string.Empty, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Equal(2, traces.PagedResult.TotalItemCount);

        var resource1Metrics = repository.GetInstrumentsSummaries(new ResourceKey("resource1", "123"));
        Assert.Single(resource1Metrics);

        // Assert - resource2 data is unaffected
        var resource2Key = new ResourceKey("resource2", "456");
        var resource2Logs = repository.GetLogs(new GetLogsContext { ResourceKey = resource2Key, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Single(resource2Logs.Items);
        Assert.Equal("log-resource2-456", resource2Logs.Items[0].Message);

        var resource2Traces = repository.GetTraces(new GetTracesRequest { ResourceKey = resource2Key, FilterText = string.Empty, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Single(resource2Traces.PagedResult.Items);

        var resource2Metrics = repository.GetInstrumentsSummaries(new ResourceKey("resource2", "456"));
        Assert.Single(resource2Metrics);
    }

    [Fact]
    public void ClearSelectedSignals_OtherResourcesRemainUnaffected()
    {
        // Arrange
        var repository = CreateRepository();

        AddTestData(repository, "resource1", "111");
        AddTestData(repository, "resource2", "222");
        AddTestData(repository, "resource3", "333");

        // Act - Clear all data types for resource2 only
        var selectedResources = new Dictionary<string, HashSet<AspireDataType>>
        {
            ["resource2-222"] = [AspireDataType.StructuredLogs, AspireDataType.Traces, AspireDataType.Metrics, AspireDataType.Resource]
        };
        repository.ClearSelectedSignals(selectedResources);

        // Assert - resource1 and resource3 data is unaffected
        var logs = repository.GetLogs(new GetLogsContext { ResourceKey = null, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Equal(2, logs.TotalItemCount);
        Assert.Contains(logs.Items, l => l.Message == "log-resource1-111");
        Assert.Contains(logs.Items, l => l.Message == "log-resource3-333");
        Assert.DoesNotContain(logs.Items, l => l.Message == "log-resource2-222");

        var traces = repository.GetTraces(new GetTracesRequest { ResourceKey = null, FilterText = string.Empty, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Equal(2, traces.PagedResult.TotalItemCount);

        var resource1Metrics = repository.GetInstrumentsSummaries(new ResourceKey("resource1", "111"));
        Assert.Single(resource1Metrics);

        var resource3Metrics = repository.GetInstrumentsSummaries(new ResourceKey("resource3", "333"));
        Assert.Single(resource3Metrics);

        // Assert - resource2 is removed from the repository since all data types were cleared
        var resource2 = repository.GetResource(new ResourceKey("resource2", "222"));
        Assert.Null(resource2);
    }

    [Fact]
    public void ClearSelectedSignals_ResourceRemovedWhenAllDataTypesCleared()
    {
        // Arrange
        var repository = CreateRepository();

        AddTestData(repository, "resource1", "123");

        // Verify resource exists before clearing
        var resourceBefore = repository.GetResource(new ResourceKey("resource1", "123"));
        Assert.NotNull(resourceBefore);

        // Act - Clear all data types for resource1
        var selectedResources = new Dictionary<string, HashSet<AspireDataType>>
        {
            ["resource1-123"] = [AspireDataType.StructuredLogs, AspireDataType.Traces, AspireDataType.Metrics, AspireDataType.Resource]
        };
        repository.ClearSelectedSignals(selectedResources);

        // Assert - Resource is removed from the repository
        var resourceAfter = repository.GetResource(new ResourceKey("resource1", "123"));
        Assert.Null(resourceAfter);

        // Assert - All telemetry data is cleared
        var logs = repository.GetLogs(new GetLogsContext { ResourceKey = null, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Empty(logs.Items);

        var traces = repository.GetTraces(new GetTracesRequest { ResourceKey = null, FilterText = string.Empty, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Empty(traces.PagedResult.Items);

        // Assert - Resources list is empty
        var resources = repository.GetResources();
        Assert.Empty(resources);
    }

    [Fact]
    public void ClearSelectedSignals_PartialClear_ResourceNotRemoved()
    {
        // Arrange
        var repository = CreateRepository();

        AddTestData(repository, "resource1", "123");

        // Act - Clear only logs and traces for resource1 (not metrics)
        var selectedResources = new Dictionary<string, HashSet<AspireDataType>>
        {
            ["resource1-123"] = [AspireDataType.StructuredLogs, AspireDataType.Traces]
        };
        repository.ClearSelectedSignals(selectedResources);

        // Assert - Resource still exists because not all data types were cleared
        var resourceAfter = repository.GetResource(new ResourceKey("resource1", "123"));
        Assert.NotNull(resourceAfter);

        // Assert - Logs and traces are cleared, but metrics remain
        var logs = repository.GetLogs(new GetLogsContext { ResourceKey = null, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Empty(logs.Items);

        var traces = repository.GetTraces(new GetTracesRequest { ResourceKey = null, FilterText = string.Empty, StartIndex = 0, Count = 10, Filters = [] });
        Assert.Empty(traces.PagedResult.Items);

        var metrics = repository.GetInstrumentsSummaries(new ResourceKey("resource1", "123"));
        Assert.Single(metrics);
    }

    private static void AddTestData(TelemetryRepository repository, string resourceName, string instanceId)
    {
        var compositeName = $"{resourceName}-{instanceId}";

        repository.AddLogs(new AddContext(), new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: resourceName, instanceId: instanceId),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(1), message: $"log-{compositeName}", severity: SeverityNumber.Error) }
                    }
                }
            }
        });

        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: resourceName, instanceId: instanceId),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: compositeName, spanId: $"{compositeName}-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        repository.AddMetrics(new AddContext(), new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: resourceName, instanceId: instanceId),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: $"metric-{compositeName}", value: 1, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });
    }
}
