// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Grpc.Core;

namespace Aspire.Dashboard.Model.Otlp;

public sealed class SpanWaterfallViewModel
{
    public required OtlpSpan Span { get; init; }
    public required double LeftOffset { get; init; }
    public required double Width { get; init; }
    public required int Depth { get; init; }
    public required bool LabelIsRight { get; init; }
    public required string? UninstrumentedPeer { get; init; }

    public string GetDisplaySummary()
    {
        // Use attributes on the span to calculate a friendly summary.
        // Optimize for common cases: HTTP, RPC, DATA, etc.
        // Fall back to the span name if we can't find anything.
        if (Span.Kind == OtlpSpanKind.Client)
        {
            if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(Span.Attributes, "http.method")))
            {
                var httpMethod = OtlpHelpers.GetValue(Span.Attributes, "http.method");
                var statusCode = OtlpHelpers.GetValue(Span.Attributes, "http.status_code");

                return $"HTTP {httpMethod?.ToUpperInvariant()} {statusCode}";
            }
            else if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(Span.Attributes, "db.system")))
            {
                var dbSystem = OtlpHelpers.GetValue(Span.Attributes, "db.system");

                return $"DATA {dbSystem} {Span.Name}";
            }
            else if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(Span.Attributes, "rpc.system")))
            {
                var rpcSystem = OtlpHelpers.GetValue(Span.Attributes, "rpc.system");
                var rpcService = OtlpHelpers.GetValue(Span.Attributes, "rpc.service");
                var rpcMethod = OtlpHelpers.GetValue(Span.Attributes, "rpc.method");

                if (string.Equals(rpcSystem, "grpc", StringComparison.OrdinalIgnoreCase))
                {
                    var grpcStatusCode = OtlpHelpers.GetValue(Span.Attributes, "rpc.grpc.status_code");

                    var summary = $"RPC {rpcService}/{rpcMethod}";
                    if (!string.IsNullOrEmpty(grpcStatusCode) && Enum.TryParse<StatusCode>(grpcStatusCode, out var statusCode))
                    {
                        summary += $" {statusCode}";
                    }
                    return summary;
                }

                return $"RPC {rpcSystem} {rpcService}/{rpcMethod}";
            }
            else if (!string.IsNullOrEmpty(OtlpHelpers.GetValue(Span.Attributes, "messaging.system")))
            {
                var messagingSystem = OtlpHelpers.GetValue(Span.Attributes, "messaging.system");
                var messagingOperation = OtlpHelpers.GetValue(Span.Attributes, "messaging.operation");
                var destinationName = OtlpHelpers.GetValue(Span.Attributes, "messaging.destination.name");

                return $"MSG {messagingSystem} {messagingOperation} {destinationName}";
            }
        }

        return Span.Name;
    }
}
