// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Api;
using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests;

public class TelemetryApiServiceTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FollowSpansAsync_WithLimit_DoesNotSkipItems()
    {
        // Arrange
        var repository = CreateRepository();

        // Add 5 spans
        for (var i = 1; i <= 5; i++)
        {
            repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>
            {
                new ResourceSpans
                {
                    Resource = CreateResource(name: "service1", instanceId: "inst1"),
                    ScopeSpans =
                    {
                        new ScopeSpans
                        {
                            Scope = CreateScope(),
                            Spans =
                            {
                                CreateSpan(traceId: $"trace{i}", spanId: $"span{i}", startTime: s_testTime.AddMinutes(i), endTime: s_testTime.AddMinutes(i + 1))
                            }
                        }
                    }
                }
            });
        }

        var service = new TelemetryApiService(repository);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - stream with limit=3
        var receivedItems = new List<string>();
        await foreach (var item in service.FollowSpansAsync(null, null, null, limit: 3, cts.Token))
        {
            receivedItems.Add(item);
            if (receivedItems.Count >= 5)
            {
                break;
            }
        }

        // Assert - should receive all 5 items (limit only affects initial batch, not total)
        Assert.Equal(5, receivedItems.Count);
    }

    [Fact]
    public async Task FollowLogsAsync_WithLimit_DoesNotSkipItems()
    {
        // Arrange
        var repository = CreateRepository();

        // Add 5 logs
        for (var i = 1; i <= 5; i++)
        {
            repository.AddLogs(new AddContext(), new RepeatedField<ResourceLogs>
            {
                new ResourceLogs
                {
                    Resource = CreateResource(name: "service1", instanceId: "inst1"),
                    ScopeLogs =
                    {
                        new ScopeLogs
                        {
                            Scope = CreateScope("TestLogger"),
                            LogRecords =
                            {
                                CreateLogRecord(time: s_testTime.AddMinutes(i), message: $"log{i}", severity: OpenTelemetry.Proto.Logs.V1.SeverityNumber.Info)
                            }
                        }
                    }
                }
            });
        }

        var service = new TelemetryApiService(repository);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - stream with limit=3
        var receivedItems = new List<string>();
        await foreach (var item in service.FollowLogsAsync(null, null, null, limit: 3, cts.Token))
        {
            receivedItems.Add(item);
            if (receivedItems.Count >= 5)
            {
                break;
            }
        }

        // Assert - should receive all 5 items (limit only affects initial batch, not total)
        Assert.Equal(5, receivedItems.Count);
    }
}
