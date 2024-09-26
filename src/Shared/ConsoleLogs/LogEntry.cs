// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ConsoleLogs;

[DebuggerDisplay("LineNumber = {LineNumber}, Timestamp = {Timestamp}, Content = {Content}, Type = {Type}")]
#if ASPIRE_DASHBOARD
public sealed class LogEntry
#else
internal sealed class LogEntry
#endif
{
    public string? Content { get; set; }
    public DateTime? Timestamp { get; set; }
    public LogEntryType Type { get; init; } = LogEntryType.Default;
    public int LineNumber { get; set; }

    public static LogEntry Create(DateTime? timestamp, string logMessage, bool isErrorMessage)
    {
        return new LogEntry
        {
            Timestamp = timestamp,
            Content = logMessage,
            Type = isErrorMessage ? LogEntryType.Error : LogEntryType.Default
        };
    }
}

#if ASPIRE_DASHBOARD
public enum LogEntryType
#else
internal enum LogEntryType
#endif
{
    Default,
    Error
}
