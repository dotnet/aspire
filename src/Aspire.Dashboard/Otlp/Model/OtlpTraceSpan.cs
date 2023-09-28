// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// Represents a Span within an Operation (Trace)
/// </summary>
public class OtlpTraceSpan
{
    public string TraceId { get; init; }
    public OtlpTraceScope TraceScope { get; init; }
    public OtlpApplication Source { get; init; }
    public string SpanId { get; init; }
    public string? ParentSpanId { get; init; }
    public string Name { get; init; }
    public string Kind { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string? Status { get; init; }
    public string? State { get; init; }
    public KeyValuePair<string, string>[] Attributes { get; }
    public List<OtlpSpanEvent> Events { get; } = new();

    public string ScopeName => TraceScope.ScopeName;
    public string ScopeSource => Source.ApplicationName;
    public TimeSpan Duration => EndTime - StartTime;

    public OtlpTraceSpan(OpenTelemetry.Proto.Trace.V1.Span s, OtlpApplication traceSource, OtlpTraceScope scope)
    {
        var id = s.SpanId?.ToHexString();
        if (id is null)
        {
            throw new ArgumentException("Span has no SpanId");
        }
        SpanId = id;
        ParentSpanId = s.ParentSpanId?.ToHexString();
        TraceId = s.TraceId.ToHexString();
        Source = traceSource;
        TraceScope = scope;
        Name = s.Name;
        Kind = s.Kind.ToString();
        StartTime = OtlpHelpers.UnixNanoSecondsToDateTime(s.StartTimeUnixNano);
        EndTime = OtlpHelpers.UnixNanoSecondsToDateTime(s.EndTimeUnixNano);
        Status = s.Status?.ToString();
        Attributes = s.Attributes.ToKeyValuePairs();
        State = s.TraceState;

        foreach (var e in s.Events)
        {
            Events.Add(new OtlpSpanEvent()
            {
                Name = e.Name,
                Time = OtlpHelpers.UnixNanoSecondsToDateTime(e.TimeUnixNano),
                Attributes = e.Attributes.ToKeyValuePairs()
            });
        }
    }
}
