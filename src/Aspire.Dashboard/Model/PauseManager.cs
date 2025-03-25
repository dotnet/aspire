// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ConsoleLogs;

namespace Aspire.Dashboard.Model;

public sealed class PauseManager
{
    private DateTime? _metricsPausedAt;
    private DateTime? _tracesPausedAt;
    private DateTime? _structuredLogsPausedAt;

    public bool ConsoleLogsPaused { get; private set; }
    private ImmutableSortedDictionary<DateTime, ConsoleLogPause> _consoleLogsPausedRanges = ImmutableSortedDictionary.Create<DateTime, ConsoleLogPause>(Comparer<DateTime>.Create((x, y) => x.CompareTo(y)));
    public ImmutableSortedDictionary<DateTime, ConsoleLogPause> ConsoleLogsPausedRanges => _consoleLogsPausedRanges;

    public void SetMetricsPaused(bool isPaused) => _metricsPausedAt = isPaused ? DateTime.UtcNow : null;

    public bool AreMetricsPaused([NotNullWhen(true)] out DateTime? pausedAt)
    {
        pausedAt = _metricsPausedAt;
        return _metricsPausedAt is not null;
    }

    public void SetTracesPaused(bool isPaused) => _tracesPausedAt = isPaused ? DateTime.UtcNow : null;

    public bool AreTracesPaused([NotNullWhen(true)] out DateTime? pausedAt)
    {
        pausedAt = _tracesPausedAt;
        return _tracesPausedAt is not null;
    }

    public void SetStructuredLogsPaused(bool isPaused) => _structuredLogsPausedAt = isPaused ? DateTime.UtcNow : null;

    public bool AreStructuredLogsPaused([NotNullWhen(true)] out DateTime? pausedAt)
    {
        pausedAt = _structuredLogsPausedAt;
        return _structuredLogsPausedAt is not null;
    }

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

    public bool IsConsoleLogFiltered(LogEntry entry, string application)
    {
        Debug.Assert(entry.Timestamp is not null, "Log entry timestamp should not be null.");

        ConsoleLogPause? foundRange = null;

        foreach (var range in _consoleLogsPausedRanges.Values)
        {
            if (range.IsOverlapping(entry.Timestamp.Value))
            {
                foundRange = range;
                break;
            }
        }

        if (foundRange is not null && foundRange.FilteredLogsByApplication.GetValueOrDefault(application)?.Contains(entry) is not true)
        {
            ImmutableInterlocked.Update(
                ref _consoleLogsPausedRanges,
                ranges => ranges.SetItem(foundRange.Start,
                    foundRange with { FilteredLogsByApplication = foundRange.FilteredLogsByApplication.SetItem(application, foundRange.FilteredLogsByApplication.GetValueOrDefault(application)?.Add(entry) ?? [entry]) }));
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
                return ranges.Add(timestamp, new ConsoleLogPause(Start: timestamp, End: null, FilteredLogsByApplication: ImmutableDictionary<string, ImmutableHashSet<LogEntry>>.Empty));
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

public record ConsoleLogPause(DateTime Start, DateTime? End, ImmutableDictionary<string, ImmutableHashSet<LogEntry>> FilteredLogsByApplication)
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
