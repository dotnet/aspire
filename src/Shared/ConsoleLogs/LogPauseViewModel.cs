// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ConsoleLogs;

#if ASPIRE_DASHBOARD
public sealed class LogPauseViewModel
#else
internal sealed class LogPauseViewModel
#endif
{
    public required DateTime StartTime { get; init; }
    public DateTime? EndTime { get; set; }
    public int FilteredCount { get; set; }

    public bool Contains(DateTime timestamp) => timestamp >= StartTime && (EndTime is null || timestamp < EndTime);
}
