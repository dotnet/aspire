// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model.MetricValues;

[DebuggerDisplay("Start = {Start}, End = {End}, Value = {Value}")]
public class MetricValue<T> : MetricValueBase where T : struct
{
    public readonly T Value;

    public MetricValue(T value, DateTime start, DateTime end) : base(start, end)
    {
        Value = value;
    }

    public override string? ToString() => Value.ToString();

    protected override MetricValueBase Clone()
    {
        return new MetricValue<T>(Value, Start, End);
    }
}
