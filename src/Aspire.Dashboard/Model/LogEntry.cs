// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Aspire.Dashboard.Model;

internal sealed partial class LogEntry
{
    private static readonly Regex s_rfc3339NanoRegEx = GenerateRfc3339NanoRegEx();

    public string? Content { get; set; }
    public string? Timestamp { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<LogEntryType>))]
    public LogEntryType Type { get; init; } = LogEntryType.Default;

    public static LogEntry Create(string s, LogEntryType type)
    {
        var indexOfSpace = s.IndexOf(' ');
        var possibleTimestamp = s[..indexOfSpace];

        // For right now, we only support RFC3339Nano timestamps, which is what (most) containers generate
        // We can tweak this once project/executable log timestamps are finalized
        if (s_rfc3339NanoRegEx.IsMatch(possibleTimestamp))
        {
            return new() { Timestamp = possibleTimestamp, Content = s[(indexOfSpace + 1)..], Type = type };
        }
        else
        {
            return new() { Content = s, Type = type };
        }
    }

    [GeneratedRegex("^(?:\\d{4})-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12][0-9]|3[01])T(?:[01][0-9]|2[0-3]):(?:[0-5][0-9]):(?:[0-5][0-9])(?:\\.\\d{1,9})(?:Z|[+-](?:[01][0-9]|2[0-3]):[0-5][0-9])?$")]
    private static partial Regex GenerateRfc3339NanoRegEx();
}

internal enum LogEntryType
{
    Default,
    Error,
    Warning
}
