// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model;

[DebuggerDisplay("Timestamp = {(Timestamp ?? ParentTimestamp),nq}, Content = {Content}")]
internal sealed partial class LogEntry
{
    public string? Content { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public LogEntryType Type { get; init; } = LogEntryType.Default;
    public int LineIndex { get; set; }
    public Guid? ParentId { get; set; }
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset? ParentTimestamp { get; set; }
    public bool IsFirstLine { get; init; }
    public int LineNumber { get; set; }
}

internal enum LogEntryType
{
    Default,
    Error
}
