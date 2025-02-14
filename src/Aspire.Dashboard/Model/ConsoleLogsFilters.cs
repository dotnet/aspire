// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ConsoleLogsFilters
{
    public DateTime? FilterAllLogsDate { get; set; }
    public Dictionary<string, DateTime> FilterResourceLogsDates { get; set; } = [];
}
