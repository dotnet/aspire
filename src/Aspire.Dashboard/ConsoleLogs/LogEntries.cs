// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.ConsoleLogs;

internal sealed class LogEntries
{
    private readonly List<LogEntry> _logEntries = new();

    public int? BaseLineNumber { get; set; }

    public void Clear() => _logEntries.Clear();

    public IList<LogEntry> GetEntries() => _logEntries;

    public void InsertSorted(LogEntry logEntry)
    {
        if (logEntry.ParentId != null)
        {
            // If we have a parent id, then we know we're on a non-timestamped line that is part
            // of a multi-line log entry. We need to find the prior line from that entry
            for (var rowIndex = _logEntries.Count - 1; rowIndex >= 0; rowIndex--)
            {
                var current = _logEntries[rowIndex];

                if (current.Id == logEntry.ParentId && logEntry.LineIndex - 1 == current.LineIndex)
                {
                    InsertLogEntry(_logEntries, rowIndex + 1, logEntry);
                    return;
                }
            }
        }
        else if (logEntry.Timestamp != null)
        {
            // Otherwise, if we have a timestamped line, we just need to find the prior line.
            // Since the rows are always in order, as soon as we see a timestamp
            // that is less than the one we're adding, we can insert it immediately after that
            for (var rowIndex = _logEntries.Count - 1; rowIndex >= 0; rowIndex--)
            {
                var current = _logEntries[rowIndex];
                var currentTimestamp = current.Timestamp ?? current.ParentTimestamp;

                if (currentTimestamp != null && currentTimestamp <= logEntry.Timestamp)
                {
                    InsertLogEntry(_logEntries, rowIndex + 1, logEntry);
                    return;
                }
            }
        }

        // If we didn't find a place to insert then append it to the end. This happens with the first entry, but
        // could also happen if the logs don't have recognized timestamps.
        InsertLogEntry(_logEntries, _logEntries.Count, logEntry);

        void InsertLogEntry(List<LogEntry> logEntries, int index, LogEntry logEntry)
        {
            // Set the line number of the log entry.
            if (index == 0)
            {
                Debug.Assert(BaseLineNumber != null, "Should be set before this method is run.");
                logEntry.LineNumber = BaseLineNumber.Value;
            }
            else
            {
                logEntry.LineNumber = logEntries[index - 1].LineNumber + 1;
            }

            logEntries.Insert(index, logEntry);

            // If a log entry isn't inserted at the end then update the line numbers of all subsequent entries.
            for (var i = index + 1; i < logEntries.Count; i++)
            {
                logEntries[i].LineNumber++;
            }
        }
    }
}
