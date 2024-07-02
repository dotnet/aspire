// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// Represents a Span within an Operation (Trace)
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public class OtlpSpan
{
    public const string PeerServiceAttributeKey = "peer.service";
    public const string UrlFullAttributeKey = "url.full";
    public const string ServerAddressAttributeKey = "server.address";
    public const string ServerPortAttributeKey = "server.port";
    public const string NetPeerNameAttributeKey = "net.peer.name";
    public const string NetPeerPortAttributeKey = "net.peer.port";
    public const string SpanKindAttributeKey = "span.kind";

    public string TraceId => Trace.TraceId;
    public OtlpTrace Trace { get; }
    public OtlpApplication Source { get; }

    public required string SpanId { get; init; }
    public required string? ParentSpanId { get; init; }
    public required string Name { get; init; }
    public required OtlpSpanKind Kind { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required OtlpSpanStatusCode Status { get; init; }
    public required string? StatusMessage { get; init; }
    public required string? State { get; init; }
    public required KeyValuePair<string, string>[] Attributes { get; init; }
    public required List<OtlpSpanEvent> Events { get; init; }

    public OtlpScope Scope { get; }
    public TimeSpan Duration => EndTime - StartTime;

    public IEnumerable<OtlpSpan> GetChildSpans() => Trace.Spans.Where(s => s.ParentSpanId == SpanId);
    public OtlpSpan? GetParentSpan() => string.IsNullOrEmpty(ParentSpanId) ? null : Trace.Spans.Where(s => s.SpanId == ParentSpanId).FirstOrDefault();

    public OtlpSpan(OtlpApplication application, OtlpTrace trace, OtlpScope scope)
    {
        Source = application;
        Trace = trace;
        Scope = scope;
    }

    public static OtlpSpan Clone(OtlpSpan item, OtlpTrace trace)
    {
        return new OtlpSpan(item.Source, trace, item.Scope)
        {
            SpanId = item.SpanId,
            ParentSpanId = item.ParentSpanId,
            Name = item.Name,
            Kind = item.Kind,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            Status = item.Status,
            StatusMessage = item.StatusMessage,
            State = item.State,
            Attributes = item.Attributes,
            Events = item.Events
        };
    }

    public Dictionary<string, string> AllProperties()
    {
        var props = new Dictionary<string, string>
        {
            { "SpanId", SpanId },
            { "Name", Name },
            { "Kind", Kind.ToString() },
        };

        if (Status != OtlpSpanStatusCode.Unset)
        {
            props.Add("Status", Status.ToString());
        }

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            props.Add("StatusMessage", StatusMessage);
        }

        foreach (var kv in Attributes.OrderBy(a => a.Key))
        {
            props.TryAdd(kv.Key, kv.Value);
        }

        return props;
    }

    private string DebuggerToString()
    {
        return $@"SpanId = {SpanId}, StartTime = {StartTime.ToLocalTime():h:mm:ss.fff tt}, ParentSpanId = {ParentSpanId}, TraceId = {Trace.TraceId}";
    }
}
