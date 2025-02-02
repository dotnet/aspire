// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;
using Grpc.Core;

namespace Aspire.Dashboard.Model.Otlp;

public sealed class SpanWaterfallViewModel
{
    public required List<SpanWaterfallViewModel> Children { get; init; }
    public required OtlpSpan Span { get; init; }
    public required double LeftOffset { get; init; }
    public required double Width { get; init; }
    public required int Depth { get; init; }
    public required bool LabelIsRight { get; init; }
    public required string? UninstrumentedPeer { get; init; }
    public bool IsHidden { get; set; }
    [MemberNotNullWhen(true, nameof(UninstrumentedPeer))]
    public bool HasUninstrumentedPeer => !string.IsNullOrEmpty(UninstrumentedPeer);
    public bool IsError => Span.Status == OtlpSpanStatusCode.Error;

    private bool _isCollapsed;
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            _isCollapsed = value;
            UpdateHidden();
        }
    }

    public string GetTooltip(List<OtlpApplication> allApplications)
    {
        var tooltip = GetTitle(Span, allApplications);
        if (IsError)
        {
            tooltip += Environment.NewLine + "Status = Error";
        }
        if (HasUninstrumentedPeer)
        {
            tooltip += Environment.NewLine + $"Outgoing call to {UninstrumentedPeer}";
        }

        return tooltip;
    }

    public static string GetTitle(OtlpSpan span, List<OtlpApplication> allApplications)
    {
        return $"{OtlpApplication.GetResourceName(span.Source, allApplications)}: {GetDisplaySummary(span)}";
    }

    public static string GetDisplaySummary(OtlpSpan span)
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

    private void UpdateHidden(bool isParentCollapsed = false)
    {
        IsHidden = isParentCollapsed;
        foreach (var child in Children)
        {
            child.UpdateHidden(isParentCollapsed || IsCollapsed);
        }
    }

    private readonly record struct SpanWaterfallViewModelState(SpanWaterfallViewModel? Parent, int Depth, bool Hidden);

    public sealed record TraceDetailState(IEnumerable<IOutgoingPeerResolver> OutgoingPeerResolvers, List<string> CollapsedSpanIds);

    public static List<SpanWaterfallViewModel> Create(OtlpTrace trace, TraceDetailState state)
    {
        var orderedSpans = new List<SpanWaterfallViewModel>();

        TraceHelpers.VisitSpans(trace, (OtlpSpan span, SpanWaterfallViewModelState s) =>
        {
            var viewModel = CreateViewModel(span, s.Depth, s.Hidden, state);
            orderedSpans.Add(viewModel);

            s.Parent?.Children.Add(viewModel);

            return s with { Parent = viewModel, Depth = s.Depth + 1, Hidden = viewModel.IsHidden || viewModel.IsCollapsed };
        }, new SpanWaterfallViewModelState(Parent: null, Depth: 1, Hidden: false));

        return orderedSpans;

        static SpanWaterfallViewModel CreateViewModel(OtlpSpan span, int depth, bool hidden, TraceDetailState state)
        {
            var traceStart = span.Trace.FirstSpan.StartTime;
            var relativeStart = span.StartTime - traceStart;
            var rootDuration = span.Trace.Duration.TotalMilliseconds;

            var leftOffset = relativeStart.TotalMilliseconds / rootDuration * 100;
            var width = span.Duration.TotalMilliseconds / rootDuration * 100;

            // Figure out if the label is displayed to the left or right of the span.
            // If the label position is based on whether more than half of the span is on the left or right side of the trace.
            var labelIsRight = (relativeStart + span.Duration / 2) < (span.Trace.Duration / 2);

            // A span may indicate a call to another service but the service isn't instrumented.
            var hasPeerService = OtlpHelpers.GetPeerAddress(span.Attributes) != null;
            var isUninstrumentedPeer = hasPeerService && span.Kind is OtlpSpanKind.Client or OtlpSpanKind.Producer && !span.GetChildSpans().Any();
            var uninstrumentedPeer = isUninstrumentedPeer ? ResolveUninstrumentedPeerName(span, state.OutgoingPeerResolvers) : null;

            var viewModel = new SpanWaterfallViewModel
            {
                Children = [],
                Span = span,
                LeftOffset = leftOffset,
                Width = width,
                Depth = depth,
                LabelIsRight = labelIsRight,
                UninstrumentedPeer = uninstrumentedPeer
            };

            // Restore hidden/collapsed state to new view model.
            if (state.CollapsedSpanIds.Contains(span.SpanId))
            {
                viewModel.IsCollapsed = true;
            }
            if (hidden)
            {
                viewModel.IsHidden = true;
            }

            return viewModel;
        }
    }

    private static string? ResolveUninstrumentedPeerName(OtlpSpan span, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        // Attempt to resolve uninstrumented peer to a friendly name from the span.
        foreach (var resolver in outgoingPeerResolvers)
        {
            if (resolver.TryResolvePeerName(span.Attributes, out var name))
            {
                return name;
            }
        }

        // Fallback to the peer address.
        return OtlpHelpers.GetPeerAddress(span.Attributes);
    }
}
