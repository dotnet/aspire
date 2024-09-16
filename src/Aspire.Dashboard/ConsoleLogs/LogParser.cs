// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.ConsoleLogs;

namespace Aspire.Dashboard.ConsoleLogs;

internal sealed class LogParser
{
    private AnsiParser.ParserState? _residualState;

    public LogEntry CreateLogEntry(string rawText, bool isErrorOutput)
    {
        // Several steps to do here:
        //
        // 1. Parse the content to look for the timestamp
        // 2. HTML Encode the raw text for security purposes
        // 3. Parse the content to look for ANSI Control Sequences and color them if possible
        // 4. Parse the content to look for URLs and make them links if possible
        // 5. Create the LogEntry

        var content = rawText;

        // 1. Parse the content to look for the timestamp
        DateTime? timestamp = null;

        if (TimestampParser.TryParseConsoleTimestamp(content, out var timestampParseResult))
        {
            content = timestampParseResult.Value.ModifiedText;
            timestamp = timestampParseResult.Value.Timestamp.UtcDateTime;
        }

        // 2. HTML Encode the raw text for security purposes
        content = WebUtility.HtmlEncode(content);

        // 3. Parse the content to look for ANSI Control Sequences and color them if possible
        var conversionResult = AnsiParser.ConvertToHtml(content, _residualState);
        content = conversionResult.ConvertedText;
        _residualState = conversionResult.ResidualState;

        // 4. Parse the content to look for URLs and make them links if possible
        if (UrlParser.TryParse(content, out var modifiedText))
        {
            content = modifiedText;
        }

        // 5. Create the LogEntry
        var logEntry = new LogEntry
        {
            Timestamp = timestamp,
            Content = content,
            Type = isErrorOutput ? LogEntryType.Error : LogEntryType.Default
        };

        return logEntry;
    }
}
