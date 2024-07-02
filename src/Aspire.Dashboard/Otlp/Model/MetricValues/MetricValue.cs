// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model.MetricValues;

[DebuggerDisplay("Start = {Start}, End = {End}, Value = {Value}")]
public class MetricValue<T>(T value, DateTime start, DateTime end) : MetricValueBase(start, end) where T : struct
{
    public readonly T Value = value;

    public override string? ToString() => Value.ToString();

    protected override MetricValueBase Clone() => new MetricValue<T>(Value, Start, End);

    internal override bool TryCompare(MetricValueBase obj, out int comparisonResult)
    {
        if (Value is IComparable a && obj is MetricValue<T> other)
        {
            comparisonResult = a.CompareTo(other.Value);
            return true;
        }

        comparisonResult = default;
        return false;
    }

    public override bool Equals(object? obj) =>
        obj is MetricValue<T> other
            && Start.Equals(other.Start)
            && Count == other.Count
            && Equals(Value, other.Value);

    public override int GetHashCode() => HashCode.Combine(Start, End, Count, Value);
}
