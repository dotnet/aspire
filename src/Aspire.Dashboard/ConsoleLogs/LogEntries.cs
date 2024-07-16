// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.ConsoleLogs;

public sealed class LogEntries
{
    private readonly List<LogEntry> _logEntries = new();

    public int? BaseLineNumber { get; set; }

    public int? MaximumEntryCount { get; set; }

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
                    InsertAt(rowIndex + 1);
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
                    InsertAt(rowIndex + 1);
                    return;
                }
            }
        }

        // If we didn't find a place to insert then append it to the end. This happens with the first entry, but
        // could also happen if the logs don't have recognized timestamps.
        InsertAt(_logEntries.Count);

        void InsertAt(int index)
        {
            // Set the line number of the log entry.
            if (index == 0)
            {
                Debug.Assert(BaseLineNumber != null, "Should be set before this method is run.");
                logEntry.LineNumber = BaseLineNumber.Value;
            }
            else
            {
                logEntry.LineNumber = _logEntries[index - 1].LineNumber + 1;
            }

            // Trim old log messages if we have a maximum and we're over it.
            if (MaximumEntryCount is not (null or 0) && _logEntries.Count >= MaximumEntryCount && index is not 0)
            {
                _logEntries.RemoveAt(0);
                index--;
            }

            // Insert the entry.
            _logEntries.Insert(index, logEntry);

            // If a log entry isn't inserted at the end then update the line numbers of all subsequent entries.
            for (var i = index + 1; i < _logEntries.Count; i++)
            {
                _logEntries[i].LineNumber++;
            }
        }
    }
}
