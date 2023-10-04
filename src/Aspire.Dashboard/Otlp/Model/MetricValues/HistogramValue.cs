// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Dashboard.Otlp.Model.MetricValues;

public class HistogramValue : MetricValueBase
{
    public ulong[] Values { get; init; }
    public double Sum { get; init; }
    public double[] ExplicitBounds { get; init; }

    public HistogramValue(ulong[] values, double sum, ulong count, DateTime start, DateTime end, double[] explicitBounds) : base(start, end)
    {
        Values = values;
        Sum = sum;
        Count = count;
        ExplicitBounds = explicitBounds;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        var first = true;
        sb.Append(CultureInfo.InvariantCulture, $"Count:{Count} Sum:{Sum} Values:");
        foreach (var v in Values)
        {
            if (!first)
            {
                sb.Append(' ');
            }
            first = false;
            sb.Append(CultureInfo.InvariantCulture, $"{v}");
        }
        return sb.ToString();
    }

    protected override MetricValueBase Clone()
    {
        return new HistogramValue(Values, Sum, Count, Start, End, ExplicitBounds);
    }
}
