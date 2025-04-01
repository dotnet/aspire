// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Dashboard.Model;

public sealed class PauseManager
{
    private DateTime? _metricsPausedAt;
    private DateTime? _tracesPausedAt;
    private DateTime? _structuredLogsPausedAt;
    private ImmutableArray<PauseInterval> _consoleLogPauseIntervals = ImmutableArray<PauseInterval>.Empty;

    public bool ConsoleLogsPaused { get; private set; }

    public ImmutableArray<PauseInterval> ConsoleLogPauseIntervals
    {
        get => _consoleLogPauseIntervals;
        private set => _consoleLogPauseIntervals = value;
    }

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

    public void SetConsoleLogsPaused(bool isPaused, DateTime timestamp)
    {
        ConsoleLogsPaused = isPaused;

        ImmutableInterlocked.Update(ref _consoleLogPauseIntervals, intervals =>
        {
            if (isPaused)
            {
                var newInterval = new PauseInterval(timestamp, null);
                return intervals.Add(newInterval);
            }
            else
            {
                Debug.Assert(intervals.Length > 0, "There should be at least one interval.");
                var lastInterval = intervals.Last();
                Debug.Assert(lastInterval.End is null, "The last interval should not have an end time if unpausing.");
                var updatedInterval = lastInterval with { End = timestamp };
                return intervals.SetItem(intervals.Length - 1, updatedInterval);
            }
        });
    }
}

public sealed record PauseInterval(DateTime Start, DateTime? End);
