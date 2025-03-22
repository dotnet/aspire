// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Dashboard.Model;

public sealed class PauseManager
{
    public bool StructuredLogsPaused { get; set; }
    public bool TracesPaused { get; set; }
    public bool MetricsPaused { get; set; }

    public bool ConsoleLogsPaused { get; private set; }

    private static readonly IComparer<DateTime> s_startTimeComparer = Comparer<DateTime>.Create((x, y) => x.CompareTo(y));
    private ImmutableSortedDictionary<DateTime, ConsoleLogPause> _consoleLogsPausedRanges = ImmutableSortedDictionary.Create<DateTime, ConsoleLogPause>(s_startTimeComparer);
    public ImmutableSortedDictionary<DateTime, ConsoleLogPause> ConsoleLogsPausedRanges => _consoleLogsPausedRanges;

    public bool TryGetConsoleLogPause(DateTime startTime, [NotNullWhen(true)] out ConsoleLogPause? pause)
    {
        if (_consoleLogsPausedRanges.TryGetValue(startTime, out var range))
        {
            pause = range;
            return true;
        }

        pause = null;
        return false;
    }

    public bool IsConsoleLogFiltered(DateTime timestamp, string application)
    {
        ConsoleLogPause? foundRange = null;

        foreach (var range in _consoleLogsPausedRanges.Values)
        {
            if (range.IsOverlapping(timestamp))
            {
                foundRange = range;
                break;
            }
        }

        if (foundRange is not null && foundRange.FilteredLogsByApplication.GetValueOrDefault(application)?.Contains(timestamp) is not true)
        {
            ImmutableInterlocked.Update(
                ref _consoleLogsPausedRanges,
                ranges => ranges.SetItem(foundRange.Start,
                    foundRange with { FilteredLogsByApplication = foundRange.FilteredLogsByApplication.SetItem(application, foundRange.FilteredLogsByApplication.GetValueOrDefault(application)?.Add(timestamp) ?? [timestamp]) }));
        }

        return foundRange is not null;
    }

    public void SetConsoleLogsPaused(bool isPaused, DateTime timestamp)
    {
        ConsoleLogsPaused = isPaused;

        ImmutableInterlocked.Update(ref _consoleLogsPausedRanges, ranges =>
        {
            if (isPaused)
            {
                return ranges.Add(timestamp, new ConsoleLogPause(Start: timestamp, End: null, FilteredLogsByApplication: ImmutableDictionary<string, ImmutableHashSet<DateTime>>.Empty));
            }
            else
            {
                Debug.Assert(ranges.Count > 0, "ConsoleLogsPausedRanges should not be empty when resuming.");
                var lastRange = ranges.Values.Last();
                if (lastRange.End is not null)
                {
                    throw new InvalidOperationException("Last range end should be null when resuming.");
                }

                return ranges.SetItem(lastRange.Start, lastRange with { End = timestamp });
            }
        });
    }
}

public record ConsoleLogPause(DateTime Start, DateTime? End, ImmutableDictionary<string, ImmutableHashSet<DateTime>> FilteredLogsByApplication)
{
    public int GetFilteredLogCount(string? application)
    {
        return application is null || !FilteredLogsByApplication.TryGetValue(application, out var filteredLogs)
            ? 0
            : filteredLogs.Count;
    }

    public bool IsOverlapping(DateTime date)
    {
        return date >= Start && (End is null || date <= End);
    }
}
