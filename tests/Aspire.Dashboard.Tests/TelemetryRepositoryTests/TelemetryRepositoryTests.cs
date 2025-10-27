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
}
