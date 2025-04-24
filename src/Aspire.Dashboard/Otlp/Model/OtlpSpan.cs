// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model.Otlp;
using Grpc.Core;

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

    public OtlpApplication? UninstrumentedPeer { get; internal set; }

    public IEnumerable<OtlpSpan> GetChildSpans() => GetChildSpans(this, Trace.Spans);
    public static IEnumerable<OtlpSpan> GetChildSpans(OtlpSpan parentSpan, OtlpSpanCollection spans) => spans.Where(s => s.ParentSpanId == parentSpan.SpanId);

    private string? _cachedDisplaySummary;

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
            UninstrumentedPeer = item.UninstrumentedPeer
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
        return $@"SpanId = {SpanId}, StartTime = {StartTime.ToLocalTime():h:mm:ss.fff tt}, ParentSpanId = {ParentSpanId}, Application = {Source.ApplicationKey}, UninstrumentedPeerApplication = {UninstrumentedPeer?.ApplicationKey}, TraceId = {Trace.TraceId}";
    }

    public string GetDisplaySummary()
    {
        return _cachedDisplaySummary ??= BuildDisplaySummary(this);

        static string BuildDisplaySummary(OtlpSpan span)
        {
            // Use attributes on the span to calculate a friendly summary.
            // Optimize for common cases: HTTP, RPC, DATA, etc.
            // Fall back to the span name if we can't find anything.
            if (span.Kind is OtlpSpanKind.Client or OtlpSpanKind.Producer or OtlpSpanKind.Consumer)
            {
                if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(span.Attributes, "http.method")))
                {
                    var httpMethod = OtlpHelpers.GetValue(span.Attributes, "http.method");
                    var statusCode = OtlpHelpers.GetValue(span.Attributes, "http.status_code");

                    return $"HTTP {httpMethod?.ToUpperInvariant()} {statusCode}";
                }
                else if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(span.Attributes, "db.system")))
                {
                    var dbSystem = OtlpHelpers.GetValue(span.Attributes, "db.system");

                    return $"DATA {dbSystem} {span.Name}";
                }
                else if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(span.Attributes, "rpc.system")))
                {
                    var rpcSystem = OtlpHelpers.GetValue(span.Attributes, "rpc.system");
                    var rpcService = OtlpHelpers.GetValue(span.Attributes, "rpc.service");
                    var rpcMethod = OtlpHelpers.GetValue(span.Attributes, "rpc.method");

                    if (string.Equals(rpcSystem, "grpc", StringComparison.OrdinalIgnoreCase))
                    {
                        var grpcStatusCode = OtlpHelpers.GetValue(span.Attributes, "rpc.grpc.status_code");

                        var summary = $"RPC {rpcService}/{rpcMethod}";
                        if (!string.IsNullOrEmpty(grpcStatusCode) && Enum.TryParse<StatusCode>(grpcStatusCode, out var statusCode))
                        {
                            summary += $" {statusCode}";
                        }

                        return summary;
                    }

                    return $"RPC {rpcService}/{rpcMethod}";
                }
                else if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(span.Attributes, "messaging.system")))
                {
                    var messagingSystem = OtlpHelpers.GetValue(span.Attributes, "messaging.system");
                    var messagingOperation = OtlpHelpers.GetValue(span.Attributes, "messaging.operation");
                    var destinationName = OtlpHelpers.GetValue(span.Attributes, "messaging.destination.name");

                    return $"MSG {messagingSystem} {messagingOperation} {destinationName}";
                }
            }

            return span.Name;
        }
    }

    public static FieldValues GetFieldValue(OtlpSpan span, string field)
    {
        // FieldValues is a hack to support two values in a single field.
        // Find a better way to do this if more than two values are needed.
        return field switch
        {
            KnownResourceFields.ServiceNameField => new FieldValues(span.Source.Application.ApplicationName, span.UninstrumentedPeer?.ApplicationName),
            KnownTraceFields.TraceIdField => span.TraceId,
            KnownTraceFields.SpanIdField => span.SpanId,
            KnownTraceFields.KindField => span.Kind.ToString(),
            KnownTraceFields.StatusField => span.Status.ToString(),
            KnownSourceFields.NameField => span.Scope.ScopeName,
            KnownTraceFields.NameField => span.Name,
            _ => span.Attributes.GetValue(field)
        };
    }

    public record struct FieldValues(string? Value1, string? Value2 = null)
    {
        public static implicit operator FieldValues(string? value) => new FieldValues(value);
    }
}
