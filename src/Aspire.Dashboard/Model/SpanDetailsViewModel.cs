// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public sealed class SpanDetailsViewModel
{
    public required OtlpSpan Span { get; init; }
    public required List<SpanPropertyViewModel> Properties { get; init; }
    public required string Title { get; init; }
}
