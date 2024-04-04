// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

internal static class TestHelpers
{
    public static void AssertId(string expected, string actual)
    {
        var bytes = Convert.FromHexString(actual);
        var resolvedActual = Encoding.UTF8.GetString(bytes);

        Assert.Equal(expected, resolvedActual);
    }

    public static string GetHexId(string text)
    {
        var id = Encoding.UTF8.GetBytes(text);
        return OtlpHelpers.ToHexString(id);
    }

    public static InstrumentationScope CreateScope(string? name = null, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        var scope = new InstrumentationScope() { Name = name ?? "TestScope" };

        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                scope.Attributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
            }
        }

        return scope;
    }

    public static Metric CreateHistogramMetric(string metricName, DateTime startTime)
    {
        return new Metric
        {
            Name = metricName,
            Description = "Test metric description",
            Unit = "widget",
            Histogram = new Histogram
            {
                AggregationTemporality = AggregationTemporality.Cumulative,
                DataPoints =
                {
                    new HistogramDataPoint
                    {
                        Count = 1,
                        Sum = 1,
                        ExplicitBounds = { 1, 2, 3 },
                        BucketCounts = { 1, 2, 3 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(startTime)
                    }
                }
            }
        };
    }

    public static Metric CreateSumMetric(string metricName, DateTime startTime, IEnumerable<KeyValuePair<string, string>>? attributes = null, int? value = null)
    {
        return new Metric
        {
            Name = metricName,
            Description = "Test metric description",
            Unit = "widget",
            Sum = new Sum
            {
                AggregationTemporality = AggregationTemporality.Cumulative,
                IsMonotonic = true,
                DataPoints =
                {
                    CreateNumberPoint(startTime, value ?? 1, attributes)
                }
            }
        };
    }

    private static NumberDataPoint CreateNumberPoint(DateTime startTime, int value, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        var point = new NumberDataPoint
        {
            AsInt = value,
            StartTimeUnixNano = DateTimeToUnixNanoseconds(startTime),
            TimeUnixNano = DateTimeToUnixNanoseconds(startTime)
        };
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                point.Attributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
            }
        }

        return point;
    }

    public static Span.Types.Event CreateSpanEvent(string name, int startTime, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        var e = new Span.Types.Event
        {
            Name = name,
            TimeUnixNano = (ulong)startTime
        };
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                e.Attributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
            }
        }

        return e;
    }

    public static Span CreateSpan(string traceId, string spanId, DateTime startTime, DateTime endTime, string? parentSpanId = null, List<Span.Types.Event>? events = null, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        var span = new Span
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(spanId)),
            ParentSpanId = parentSpanId is null ? ByteString.Empty : ByteString.CopyFrom(Encoding.UTF8.GetBytes(parentSpanId)),
            StartTimeUnixNano = DateTimeToUnixNanoseconds(startTime),
            EndTimeUnixNano = DateTimeToUnixNanoseconds(endTime),
            Name = "Test span"
        };
        if (events != null)
        {
            span.Events.AddRange(events);
        }
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                span.Attributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
            }
        }

        return span;
    }

    public static LogRecord CreateLogRecord(DateTime? time = null, string? message = null, SeverityNumber? severity = null, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        attributes ??= [new KeyValuePair<string, string>("{OriginalFormat}", "Test {Log}"), new KeyValuePair<string, string>("Log", "Value!")];

        var logRecord = new LogRecord
        {
            Body = new AnyValue { StringValue = message ?? "Test Value!" },
            TraceId = ByteString.CopyFrom(Convert.FromHexString("5465737454726163654964")),
            SpanId = ByteString.CopyFrom(Convert.FromHexString("546573745370616e4964")),
            TimeUnixNano = time != null ? DateTimeToUnixNanoseconds(time.Value) : 1000,
            SeverityNumber = severity ?? SeverityNumber.Info
        };

        foreach (var attribute in attributes)
        {
            logRecord.Attributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
        }

        return logRecord;
    }

    public static Resource CreateResource(string? name = null, string? instanceId = null)
    {
        return new Resource()
        {
            Attributes =
            {
                new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = name ?? "TestService" } },
                new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = instanceId ?? "TestId" } }
            }
        };
    }

    public static TelemetryRepository CreateRepository(
        int? maxMetricsCount = null,
        int? maxAttributeCount = null,
        int? maxAttributeLength = null,
        int? maxSpanEventCount = null,
        TimeSpan? subscriptionMinExecuteInterval = null)
    {
        var options = new TelemetryLimitOptions();
        if (maxMetricsCount != null)
        {
            options.MaxMetricsCount = maxMetricsCount.Value;
        }
        if (maxAttributeCount != null)
        {
            options.MaxAttributeCount = maxAttributeCount.Value;
        }
        if (maxAttributeLength != null)
        {
            options.MaxAttributeLength = maxAttributeLength.Value;
        }
        if (maxSpanEventCount != null)
        {
            options.MaxSpanEventCount = maxSpanEventCount.Value;
        }

        var repository = new TelemetryRepository(NullLoggerFactory.Instance, Options.Create(new DashboardOptions { TelemetryLimits = options }));
        if (subscriptionMinExecuteInterval != null)
        {
            repository._subscriptionMinExecuteInterval = subscriptionMinExecuteInterval.Value;
        }
        return repository;
    }

    public static ulong DateTimeToUnixNanoseconds(DateTime dateTime)
    {
        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeSinceEpoch = dateTime.ToUniversalTime() - unixEpoch;

        return (ulong)timeSinceEpoch.Ticks * 100;
    }

    public static string GetValue(int valueLength)
    {
        var value = new StringBuilder(valueLength);
        for (var i = 0; i < valueLength; i++)
        {
            value.Append((i % 10).ToString(CultureInfo.InvariantCulture));
        }

        return value.ToString();
    }
}
