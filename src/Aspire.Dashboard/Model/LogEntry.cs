// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Model;

internal sealed class LogEntry
{
    public string? Content { get; set; }
    public string? Timestamp { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<LogEntryType>))]
    public LogEntryType Type { get; init; } = LogEntryType.Default;
}

internal enum LogEntryType
{
    Default,
    Error,
    Warning
}
