// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model.MetricValues;

[DebuggerDisplay("Start = {Start}, End = {End}, Exemplars = {Exemplars.Count}")]
public abstract class MetricValueBase
{
    private List<MetricsExemplar>? _exemplars;

    public List<MetricsExemplar> Exemplars => _exemplars ??= new List<MetricsExemplar>();
    public bool HasExemplars => _exemplars != null && _exemplars.Count > 0;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public ulong Count = 1;

    protected MetricValueBase(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    internal static MetricValueBase Clone(MetricValueBase item)
    {
        return item.Clone();
    }

    internal abstract bool TryCompare(MetricValueBase other, out int comparisonResult);

    protected abstract MetricValueBase Clone();
}

[DebuggerDisplay("Start = {Start}, Value = {Value}, SpanId = {SpanId}, TraceId = {TraceId}, Attributes = {Attributes.Count}")]
public sealed class MetricsExemplar
{
    public required DateTime Start { get; init; }
    public required double Value { get; init; }
    public required string SpanId { get; init; }
    public required string TraceId { get; init; }
    public required KeyValuePair<string, string>[] Attributes { get; init; }
}
