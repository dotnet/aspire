// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.ConsoleLogs;

internal sealed partial class LogParser(bool convertTimestampsFromUtc)
{
    private string? _parentTimestamp;
    private Guid? _parentId;
    private int _lineIndex;
    private AnsiParser.ParserState? _residualState;

    public LogEntry CreateLogEntry(string rawText, bool isErrorOutput)
    {
        // Several steps to do here:
        //
        // 1. HTML Encode the raw text for security purposes
        // 2. Parse the content to look for the timestamp and color it if possible
        // 3. Parse the content to look for info/warn/dbug header
        // 4. Parse the content to look for ANSI Control Sequences and color them if possible
        // 5. Parse the content to look for URLs and make them links if possible
        // 6. Create the LogEntry to get the ID
        // 7. Set the relative properties of the log entry (parent/line index/etc)
        // 8. Return the final result

        // 1. HTML Encode the raw text for security purposes
        var content = WebUtility.HtmlEncode(rawText);

        // 2. Parse the content to look for the timestamp and color it if possible
        var isFirstLine = false;
        string? timestamp = null;

        if (TimestampParser.TryColorizeTimestamp(content, convertTimestampsFromUtc, out var timestampParseResult))
        {
            isFirstLine = true;
            content = timestampParseResult.ModifiedText;
            timestamp = timestampParseResult.Timestamp;
        }
        // 3. Parse the content to look for info/warn/dbug header
        // TODO extract log level and use here
        else if (LogLevelParser.StartsWithLogLevelHeader(content))
        {
            isFirstLine = true;
        }

        // 4. Parse the content to look for ANSI Control Sequences and color them if possible
        var conversionResult = AnsiParser.ConvertToHtml(content, _residualState);
        content = conversionResult.ConvertedText;
        _residualState = conversionResult.ResidualState;

        // 5. Parse the content to look for URLs and make them links if possible
        if (UrlParser.TryParse(content, out var modifiedText))
        {
            content = modifiedText;
        }

        // 6. Create the LogEntry to get the ID
        var logEntry = new LogEntry()
        {
            Timestamp = timestamp,
            Content = content,
            Type = isErrorOutput ? LogEntryType.Error : LogEntryType.Default,
            IsFirstLine = isFirstLine
        };

        // 7. Set the relative properties of the log entry (parent/line index/etc)
        if (isFirstLine)
        {
            _parentTimestamp = logEntry.Timestamp;
            _parentId = logEntry.Id;
            _lineIndex = 0;
        }
        else if (_parentId.HasValue)
        {
            logEntry.ParentTimestamp = _parentTimestamp;
            logEntry.ParentId = _parentId;
            logEntry.LineIndex = ++_lineIndex;
        }

        // 8. Return the final result
        return logEntry;
    }
}
