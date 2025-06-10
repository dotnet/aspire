// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Hosting.ConsoleLogs;

namespace Aspire.Dashboard.ConsoleLogs;

internal sealed class LogParser
{
    private readonly ConsoleColor _defaultBackgroundColor;
    private AnsiParser.ParserState? _residualState;

    public LogParser(ConsoleColor defaultBackgroundColor)
    {
        _defaultBackgroundColor = defaultBackgroundColor;
    }

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

        Func<string, string> callback = (s) =>
        {
            // This callback is run on text that isn't transformed into a clickable URL.

            // 3a. HTML Encode the raw text for security purposes
            var updatedText = WebUtility.HtmlEncode(s);

            // 3b. Parse the content to look for ANSI Control Sequences and color them if possible
            var conversionResult = AnsiParser.ConvertToHtml(updatedText, _residualState, _defaultBackgroundColor);
            updatedText = conversionResult.ConvertedText;
            _residualState = conversionResult.ResidualState;

            return updatedText ?? string.Empty;
        };

        // 3. Parse the content to look for URLs and make them links if possible
        if (UrlParser.TryParse(content, callback, out var modifiedText))
        {
            content = modifiedText;
        }
        else
        {
            content = callback(content);
        }

        // 5. Create the LogEntry
        var logEntry = LogEntry.Create(timestamp, content, rawText, isErrorOutput);

        return logEntry;
    }
}
