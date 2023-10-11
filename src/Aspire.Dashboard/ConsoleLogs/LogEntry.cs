// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Model;

internal sealed partial class LogEntry
{
    public string? Content { get; set; }
    public string? Timestamp { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<LogEntryType>))]
    public LogEntryType Type { get; init; } = LogEntryType.Default;
    public int LineIndex { get; set; }
    public Guid? ParentId { get; set; }
    public Guid Id { get; } = Guid.NewGuid();
    public string? ParentTimestamp { get; set; }
    public bool IsFirstLine { get; init; }    
}

internal enum LogEntryType
{
    Default,
    Error,
    Warning
}
