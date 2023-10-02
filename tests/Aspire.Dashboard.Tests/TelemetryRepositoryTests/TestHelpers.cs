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

    public static InstrumentationScope CreateScope()
    {
        return new InstrumentationScope() { Name = "TestScope" };
    }

    public static Span CreateSpan(string traceId, string spanId, DateTime startTime, DateTime endTime, string? parentSpanId = null)
    {
        return new Span
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(spanId)),
            ParentSpanId = parentSpanId is null ? ByteString.Empty : ByteString.CopyFrom(Encoding.UTF8.GetBytes(parentSpanId)),
            StartTimeUnixNano = DateTimeToUnixNanoseconds(startTime),
            EndTimeUnixNano = DateTimeToUnixNanoseconds(endTime),
            Name = "Test span"
        };
    }

    public static LogRecord CreateLogRecord(DateTime? time = null, string? message = null)
    {
        return new LogRecord
        {
            Body = new AnyValue { StringValue = message ?? "Test Value!" },
            TraceId = ByteString.CopyFrom(Convert.FromHexString("5465737454726163654964")),
            SpanId = ByteString.CopyFrom(Convert.FromHexString("546573745370616e4964")),
            TimeUnixNano = time != null ? DateTimeToUnixNanoseconds(time.Value) : 1000,
            SeverityNumber = SeverityNumber.Info,
            Attributes =
            {
                new KeyValue { Key = "{OriginalFormat}", Value = new AnyValue { StringValue = "Test {Log}" } },
                new KeyValue { Key = "ParentId", Value = new AnyValue { StringValue = "TestParentId" } },
                new KeyValue { Key = "Log", Value = new AnyValue { StringValue = "Value!" } }
            }
        };
    }

    public static Resource CreateResource()
    {
        return new Resource()
        {
            Attributes =
            {
                new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "TestService" } },
                new KeyValue { Key = "service.instance.id", Value = new AnyValue { StringValue = "TestId" } }
            }
        };
    }

    public static TelemetryRepository CreateRepository()
    {
        return new TelemetryRepository(new ConfigurationManager(), NullLoggerFactory.Instance);
    }

    private static ulong DateTimeToUnixNanoseconds(DateTime dateTime)
    {
        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeSinceEpoch = dateTime.ToUniversalTime() - unixEpoch;

        return (ulong)timeSinceEpoch.Ticks / 100;
    }
}
