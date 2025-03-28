// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Dashboard.Model;

public sealed class PauseManager
{
    private DateTime? _metricsPausedAt;
    private DateTime? _tracesPausedAt;
    private DateTime? _structuredLogsPausedAt;

    public bool ConsoleLogsPaused { get; private set; }

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

    public void SetConsoleLogsPaused(bool isPaused)
    {
        ConsoleLogsPaused = isPaused;
    }
}
