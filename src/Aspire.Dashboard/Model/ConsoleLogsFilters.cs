// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Immutable filters for console logs. This type is serialized to browser storage.
/// </summary>
public sealed record ConsoleLogsFilters
{
    public DateTime? FilterAllLogsDate { get; init; }
    public ImmutableDictionary<string, DateTime> FilterResourceLogsDates { get; init; } = ImmutableDictionary<string, DateTime>.Empty;

    /// <summary>
    /// Creates new filters with all logs cleared at the specified date.
    /// </summary>
    public static ConsoleLogsFilters CreateClearAll(DateTime clearDate) =>
        new() { FilterAllLogsDate = clearDate, FilterResourceLogsDates = ImmutableDictionary<string, DateTime>.Empty };

    /// <summary>
    /// Creates new filters based on this instance with a specific resource cleared at the specified date.
    /// </summary>
    public ConsoleLogsFilters WithResourceCleared(string resourceName, DateTime clearDate) =>
        this with { FilterResourceLogsDates = FilterResourceLogsDates.SetItem(resourceName, clearDate) };
}
