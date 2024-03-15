// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Extensions;
using Aspire.Hosting;

namespace Aspire.Dashboard.ConsoleLogs;

public static partial class TimestampParser
{
    public static bool TryColorizeTimestamp(TimeProvider timeProvider, string text, bool convertTimestampsFromUtc, out TimestampParserResult result)
    {
        var match = LogParsingConstants.Rfc3339RegEx.Match(text);

        if (match.Success)
        {
            var span = text.AsSpan();
            var timestamp = span[match.Index..(match.Index + match.Length)];
            var theRest = match.Index + match.Length >= span.Length ? "" : span[(match.Index + match.Length)..];

            var timestampForDisplay = convertTimestampsFromUtc ? ConvertTimestampFromUtc(timeProvider, timestamp) : timestamp.ToString();

            var modifiedText = $"<span class=\"timestamp\">{timestampForDisplay}</span>{theRest}";
            result = new(modifiedText, timestamp.ToString());
            return true;
        }

        result = default;
        return false;
    }

    private static string ConvertTimestampFromUtc(TimeProvider timeProvider, ReadOnlySpan<char> timestamp)
    {
        if (DateTimeOffset.TryParse(timestamp, out var dateTimeUtc))
        {
            var dateTimeLocal = timeProvider.ToLocal(dateTimeUtc);
            return dateTimeLocal.ToString(KnownFormats.ConsoleLogsTimestampFormat, CultureInfo.CurrentCulture);
        }

        return timestamp.ToString();
    }

    public readonly record struct TimestampParserResult(string ModifiedText, string Timestamp);
}
