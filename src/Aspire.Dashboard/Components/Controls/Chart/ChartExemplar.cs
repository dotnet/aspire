﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Components.Controls.Chart;

[DebuggerDisplay("Start = {Start}, Value = {Value}, TraceId = {TraceId}, SpanId = {SpanId}")]
public class ChartExemplar
{
    public required DateTimeOffset Start { get; init; }
    public required double Value { get; init; }
    public required string TraceId { get; init; }
    public required string SpanId { get; init; }
    public required OtlpSpan? Span { get; init; }
}
