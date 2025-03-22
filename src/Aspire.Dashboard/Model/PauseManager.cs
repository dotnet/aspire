// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class PauseManager
{
    public bool StructuredLogsPaused { get; set; }
    public bool TracesPaused { get; set; }
    public bool MetricsPaused { get; set; }

    public bool ConsoleLogsPaused { get; set; }
    public List<DateTimeRange> ConsoleLogsPausedRanges { get; } = [];
}

public class DateTimeRange(DateTime start, DateTime? end)
{
    public DateTime Start { get; } = start;
    public DateTime? End { get; set; } = end;

    public bool IsOverlapping(DateTime date)
    {
        return date >= Start && (End is null || date <= End);
    }
}
