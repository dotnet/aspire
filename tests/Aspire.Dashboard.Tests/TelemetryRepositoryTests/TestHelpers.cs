// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

    public static InstrumentationScope CreateScope(string? name = null)
    {
        return new InstrumentationScope() { Name = name ?? "TestScope" };
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

    public static Metric CreateSumMetric(string metricName, DateTime startTime, KeyValuePair<string, string>[]? attributes = null)
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
                    CreateNumberPoint(startTime, attributes)
                }
            }
        };
    }

    private static NumberDataPoint CreateNumberPoint(DateTime startTime, KeyValuePair<string, string>[]? attributes = null)
    {
        var point = new NumberDataPoint
        {
            AsInt = 1,
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

    public static Span CreateSpan(string traceId, string spanId, DateTime startTime, DateTime endTime, string? parentSpanId = null, List<Span.Types.Event>? events = null)
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

        return span;
    }

    public static LogRecord CreateLogRecord(DateTime? time = null, string? message = null, SeverityNumber? severity = null)
    {
        return new LogRecord
        {
            Body = new AnyValue { StringValue = message ?? "Test Value!" },
            TraceId = ByteString.CopyFrom(Convert.FromHexString("5465737454726163654964")),
            SpanId = ByteString.CopyFrom(Convert.FromHexString("546573745370616e4964")),
            TimeUnixNano = time != null ? DateTimeToUnixNanoseconds(time.Value) : 1000,
            SeverityNumber = severity ?? SeverityNumber.Info,
            Attributes =
            {
                new KeyValue { Key = "{OriginalFormat}", Value = new AnyValue { StringValue = "Test {Log}" } },
                new KeyValue { Key = "Log", Value = new AnyValue { StringValue = "Value!" } }
            }
        };
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

    public static TelemetryRepository CreateRepository()
    {
        return new TelemetryRepository(new ConfigurationManager(), NullLoggerFactory.Instance);
    }

    public static ulong DateTimeToUnixNanoseconds(DateTime dateTime)
    {
        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeSinceEpoch = dateTime.ToUniversalTime() - unixEpoch;

        return (ulong)timeSinceEpoch.Ticks * 100;
    }
}
