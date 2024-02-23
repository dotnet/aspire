// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model.MetricValues;

[DebuggerDisplay("Start = {Start}, End = {End}")]
public abstract class MetricValueBase
{
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
