// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("TraceId = {TraceId}, SpanId = {SpanId}, SourceTraceId = {SourceTraceId}, SourceSpanId = {SourceSpanId}")]
public class OtlpSpanLink
{
    public required string SourceTraceId { get; init; }
    public required string SourceSpanId { get; init; }
    public required string TraceState { get; init; }
    public required string SpanId { get; init; }
    public required string TraceId { get; init; }
    public required KeyValuePair<string, string>[] Attributes { get; init; }
}
