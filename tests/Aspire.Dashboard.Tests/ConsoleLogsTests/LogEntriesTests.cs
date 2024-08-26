// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Aspire.Hosting.ConsoleLogs;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class LogEntriesTests
{
    private static LogEntries CreateLogEntries(int? maximumEntryCount = null, int? baseLineNumber = null)
    {
        var logEntries = new LogEntries(maximumEntryCount: maximumEntryCount ?? int.MaxValue);
        logEntries.BaseLineNumber = baseLineNumber ?? 1;
        return logEntries;
    }

    private static void AddLogLine(LogEntries logEntries, string content, bool isError)
    {
        var logParser = new LogParser();
        var logEntry = logParser.CreateLogEntry(content, isError);
        logEntries.InsertSorted(logEntry);
    }

    [Fact]
    public void AddLogLine_Single()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);

        // Assert
        var entry = Assert.Single(logEntries.GetEntries());
        Assert.Equal("Hello world", entry.Content);
        Assert.Null(entry.Timestamp);
    }

    [Fact]
    public void AddLogLine_MultipleLines()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);
        AddLogLine(logEntries, "Hello world 2", isError: false);
        AddLogLine(logEntries, "Hello world 3", isError: true);

        // Assert
        Assert.Collection(logEntries.GetEntries(),
            l => Assert.Equal("Hello world", l.Content),
            l => Assert.Equal("Hello world 2", l.Content),
            l => Assert.Equal("Hello world 3", l.Content));
    }

    [Fact]
    public void AddLogLine_MultipleLines_MixDatePrefix()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:01.000Z Hello world 2", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:02.000Z Hello world 3", isError: false);
        AddLogLine(logEntries, "Hello world 4", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:03.000Z Hello world 5", isError: false);

        // Assert
        var entries = logEntries.GetEntries();
        Assert.Collection(entries,
            l =>
            {
                Assert.Equal("Hello world", l.Content);
                Assert.Equal(1, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 2", l.Content);
                Assert.Equal(2, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 3", l.Content);
                Assert.Equal(3, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 4", l.Content);
                Assert.Equal(4, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 5", l.Content);
                Assert.Equal(5, l.LineNumber);
            });
    }

    [Fact]
    public void AddLogLine_MultipleLines_MixDatePrefix_OutOfOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:12:00.000Z Hello world 2", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:11:00.000Z Hello world 3", isError: false);
        AddLogLine(logEntries, "Hello world 4", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:13:00.000Z Hello world 5", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:00.000Z Hello world 6", isError: false);

        // Assert
        var entries = logEntries.GetEntries();
        Assert.Collection(entries,
            l =>
            {
                Assert.Equal("Hello world", l.Content);
                Assert.Equal(1, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 6", l.Content);
                Assert.Equal(2, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 3", l.Content);
                Assert.Equal(3, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 2", l.Content);
                Assert.Equal(4, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 4", l.Content);
                Assert.Equal(5, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 5", l.Content);
                Assert.Equal(6, l.LineNumber);
            });
    }

    [Fact]
    public void AddLogLine_MultipleLines_SameDate_InOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "2024-08-19T06:10:00.000Z Hello world 1", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:00.000Z Hello world 2", isError: false);

        // Assert
        var entries = logEntries.GetEntries();
        Assert.Collection(entries,
            l =>
            {
                Assert.Equal("Hello world 1", l.Content);
                Assert.Equal(1, l.LineNumber);
            },
            l =>
            {
                Assert.Equal("Hello world 2", l.Content);
                Assert.Equal(2, l.LineNumber);
            });
    }

    [Fact]
    public void InsertSorted_OutOfOrderWithSameTimestamp_ReturnInOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        var timestamp = DateTime.UtcNow;

        // Act
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(1), Content = "1" });
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(3), Content = "3" });
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(2), Content = "2" });

        // Assert
        var entries = logEntries.GetEntries();
        Assert.Collection(entries,
            l => Assert.Equal("1", l.Content),
            l => Assert.Equal("2", l.Content),
            l => Assert.Equal("3", l.Content));
    }

    [Fact]
    public void InsertSorted_TrimsToMaximumEntryCount_Ordered()
    {
        // Arrange
        var logEntries = CreateLogEntries(maximumEntryCount: 2);

        var timestamp = DateTime.UtcNow;

        // Act
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(1), Content = "1" });
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(2), Content = "2" });
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(3), Content = "3" });

        // Assert
        var entries = logEntries.GetEntries();
        Assert.Collection(entries,
            l => Assert.Equal("2", l.Content),
            l => Assert.Equal("3", l.Content));
    }

    [Fact]
    public void InsertSorted_TrimsToMaximumEntryCount_OutOfOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries(maximumEntryCount: 2);

        var timestamp = DateTime.UtcNow;

        // Act
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(1), Content = "1" });
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(3), Content = "3" });
        logEntries.InsertSorted(new LogEntry { Timestamp = timestamp.AddSeconds(2), Content = "2" });

        // Assert
        var entries = logEntries.GetEntries();
        Assert.Collection(entries,
            l => Assert.Equal("2", l.Content),
            l => Assert.Equal("3", l.Content));
    }
}
