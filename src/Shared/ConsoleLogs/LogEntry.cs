// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ConsoleLogs;

[DebuggerDisplay("LineNumber = {LineNumber}, Timestamp = {Timestamp}, Content = {Content}, Type = {Type}")]
internal sealed class LogEntry
{
    public string? Content { get; set; }
    public DateTime? Timestamp { get; set; }
    public LogEntryType Type { get; init; } = LogEntryType.Default;
    public int LineNumber { get; set; }
}

internal enum LogEntryType
{
    Default,
    Error
}
