// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public sealed class SpanLinkViewModel
{
    public required string TraceId { get; init; }
    public required string SpanId { get; init; }
    public required KeyValuePair<string, string>[] Attributes { get; init; }
    public required OtlpSpan? Span { get; init; }
}
