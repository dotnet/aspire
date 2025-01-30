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
    public string? Content { get; private set; }

    /// <summary>
    /// The text content of the log entry. This is the same as <see cref="Content"/>, but without embedded links or other transformations and including the timestamp.
    /// </summary>
    public string? RawContent { get; private set; }
    public DateTime? Timestamp { get; private set; }
    public LogEntryType Type { get; private set; } = LogEntryType.Default;
    public int LineNumber { get; set; }

    public static LogEntry Create(DateTime? timestamp, string logMessage, bool isErrorMessage)
    {
        return Create(timestamp, logMessage, logMessage, isErrorMessage);
    }

    public static LogEntry Create(DateTime? timestamp, string logMessage, string rawLogContent, bool isErrorMessage)
    {
        return new LogEntry
        {
            Timestamp = timestamp,
            Content = logMessage,
            RawContent = rawLogContent,
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
