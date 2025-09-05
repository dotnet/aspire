// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public class SpanHasAttributeTelemetryFilter : TelemetryFilter
{
    private readonly string[] _attributeNames;

    public SpanHasAttributeTelemetryFilter(string[] attributeNames)
    {
        _attributeNames = attributeNames;
    }

    public override IEnumerable<OtlpLogEntry> Apply(IEnumerable<OtlpLogEntry> input)
    {
        throw new NotSupportedException();
    }

    public override bool Apply(OtlpSpan span)
    {
        foreach (var attributeName in _attributeNames)
        {
            if (!string.IsNullOrEmpty(span.Attributes.GetValue(attributeName)))
            {
                return true;
            }
        }

        return false;
    }

    public override bool Equals(TelemetryFilter? other)
    {
        return false;
    }
}

public class SpanNoAttributeTelemetryFilter : TelemetryFilter
{
    private readonly string[] _attributeNames;

    public SpanNoAttributeTelemetryFilter(string[] attributeNames)
    {
        _attributeNames = attributeNames;
    }

    public override IEnumerable<OtlpLogEntry> Apply(IEnumerable<OtlpLogEntry> input)
    {
        throw new NotSupportedException();
    }

    public override bool Apply(OtlpSpan span)
    {
        foreach (var attributeName in _attributeNames)
        {
            if (!string.IsNullOrEmpty(span.Attributes.GetValue(attributeName)))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(TelemetryFilter? other)
    {
        return false;
    }
}

public class SpanType
{
    public string Name { get; }
    public TelemetryFilter Filter { get; }

    private SpanType(string name, TelemetryFilter filter)
    {
        Name = name;
        Filter = filter;
    }

    // https://opentelemetry.io/docs/specs/semconv/http/http-spans/
    public static readonly SpanType Http = new SpanType(
        "http",
        new SpanHasAttributeTelemetryFilter(["http.request.method"]));

    // https://opentelemetry.io/docs/specs/semconv/database/database-spans/
    public static readonly SpanType Database = new SpanType(
        "database",
        new SpanHasAttributeTelemetryFilter(["db.system.name", "db.system"]));

    // https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/
    public static readonly SpanType Messaging = new SpanType(
        "messaging",
        new SpanHasAttributeTelemetryFilter(["messaging.system"]));

    // https://opentelemetry.io/docs/specs/semconv/rpc/rpc-spans/
    public static readonly SpanType Rpc = new SpanType(
        "rpc",
        new SpanHasAttributeTelemetryFilter(["rpc.system"]));

    // https://opentelemetry.io/docs/specs/semconv/gen-ai/gen-ai-spans/
    public static readonly SpanType GenAI = new SpanType("genai",
        new SpanHasAttributeTelemetryFilter(["gen_ai.operation.name"]));

    public static readonly SpanType Other = new SpanType(
        "other",
        new SpanNoAttributeTelemetryFilter([
            "http.request.method",
            "db.system.name",
            "db.system",
            "messaging.system",
            "rpc.system",
            "gen_ai.operation.name"
        ]));
}

