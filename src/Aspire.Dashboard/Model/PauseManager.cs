// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Aspire.Dashboard.Model;

public sealed class PauseManager
{
    public bool StructuredLogsPaused { get; set; }
    public bool TracesPaused { get; set; }
    public bool MetricsPaused { get; set; }

    public bool ConsoleLogsPaused { get; private set; }

    private ImmutableArray<DateTimeRange> _consoleLogsPausedRanges = [];

    public bool IsConsoleLogFiltered(DateTime timestamp)
    {
        foreach (var range in _consoleLogsPausedRanges)
        {
            if (range.IsOverlapping(timestamp))
            {
                return true;
            }
        }

        return false;
    }

    public void SetConsoleLogsPaused(bool isPaused)
    {
        ConsoleLogsPaused = isPaused;

        ImmutableInterlocked.Update(ref _consoleLogsPausedRanges, ranges =>
        {
            if (isPaused)
            {
                return ranges.Add(new DateTimeRange(Start: DateTime.UtcNow, End: null));
            }
            else
            {
                Debug.Assert(ranges.Length > 0, "ConsoleLogsPausedRanges should not be empty when resuming.");
                var lastRange = ranges.Last();
                if (lastRange.End is not null)
                {
                    throw new InvalidOperationException("Last range end should be null when resuming.");
                }

                return ranges.Replace(lastRange, lastRange with { End = DateTime.UtcNow });
            }
        });
    }
}

public record DateTimeRange(DateTime Start, DateTime? End)
{
    public bool IsOverlapping(DateTime date)
    {
        return date >= Start && (End is null || date <= End);
    }
}
