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
        Children.ForEach(child => child.UpdateHidden(isParentCollapsed || IsCollapsed));
    }
}
