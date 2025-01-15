// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model.Otlp;

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
    public OtlpApplicationView Source { get; }

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
    public required List<OtlpSpanLink> Links { get; init; }
    public required List<OtlpSpanLink> BackLinks { get; init; }

    public OtlpScope Scope { get; }
    public TimeSpan Duration => EndTime - StartTime;

    public IEnumerable<OtlpSpan> GetChildSpans() => GetChildSpans(this, Trace.Spans);
    public static IEnumerable<OtlpSpan> GetChildSpans(OtlpSpan parentSpan, OtlpSpanCollection spans) => spans.Where(s => s.ParentSpanId == parentSpan.SpanId);
    public OtlpSpan? GetParentSpan()
    {
        if (string.IsNullOrEmpty(ParentSpanId))
        {
            return null;
        }

        if (Trace.Spans.TryGetValue(ParentSpanId, out var span))
        {
            return span;
        }

        return null;
    }

    public OtlpSpan(OtlpApplicationView applicationView, OtlpTrace trace, OtlpScope scope)
    {
        Source = applicationView;
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
            Events = item.Events,
            Links = item.Links,
            BackLinks = item.BackLinks,
        };
    }

    public List<OtlpDisplayField> AllProperties()
    {
        var props = new List<OtlpDisplayField>
        {
            new OtlpDisplayField { DisplayName = "SpanId", Key = KnownTraceFields.SpanIdField, Value = SpanId },
            new OtlpDisplayField { DisplayName = "Name", Key = KnownTraceFields.NameField, Value = Name },
            new OtlpDisplayField { DisplayName = "Kind", Key = KnownTraceFields.KindField, Value = Kind.ToString() },
        };

        if (Status != OtlpSpanStatusCode.Unset)
        {
            props.Add(new OtlpDisplayField { DisplayName = "Status", Key = KnownTraceFields.StatusField, Value = Status.ToString() });
        }

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            props.Add(new OtlpDisplayField { DisplayName = "StatusMessage", Key = KnownTraceFields.StatusMessageField, Value = StatusMessage });
        }

        foreach (var kv in Attributes.OrderBy(a => a.Key))
        {
            props.Add(new OtlpDisplayField { DisplayName = kv.Key, Key = $"unknown-{kv.Key}", Value = kv.Value });
        }

        return props;
    }

    private string DebuggerToString()
    {
        return $@"SpanId = {SpanId}, StartTime = {StartTime.ToLocalTime():h:mm:ss.fff tt}, ParentSpanId = {ParentSpanId}, TraceId = {Trace.TraceId}";
    }

    public static string? GetFieldValue(OtlpSpan span, string field)
    {
        return field switch
        {
            KnownResourceFields.ServiceNameField => span.Source.Application.ApplicationName,
            KnownTraceFields.TraceIdField => span.TraceId,
            KnownTraceFields.SpanIdField => span.SpanId,
            KnownTraceFields.KindField => span.Kind.ToString(),
            KnownTraceFields.StatusField => span.Status.ToString(),
            KnownSourceFields.NameField => span.Scope.ScopeName,
            KnownTraceFields.NameField => span.Name,
            _ => span.Attributes.GetValue(field)
        };
    }
}
