// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class LogViewerViewModel
{
    public List<LogEntry> LogEntries { get; } = [];
    public int? BaseLineNumber { get; set; }
    public string? ResourceName { get; set; }

}
