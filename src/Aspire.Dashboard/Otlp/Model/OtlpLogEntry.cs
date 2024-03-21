// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Otlp.Storage;
using OpenTelemetry.Proto.Logs.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("TimeStamp = {TimeStamp}, Severity = {Severity}, Message = {Message}")]
public class OtlpLogEntry
{
    public KeyValuePair<string, string>[] Attributes { get; }
    public DateTime TimeStamp { get; }
    public uint Flags { get; }
    public LogLevel Severity { get; }
    public string Message { get; }
    public string SpanId { get; }
    public string TraceId { get; }
    public string ParentId { get; }
    public string? OriginalFormat { get; }
    public OtlpApplication Application { get; }
    public OtlpScope Scope { get; }

    public OtlpLogEntry(LogRecord record, OtlpApplication logApp, OtlpScope scope, TelemetryOptions options)
    {
        string? originalFormat = null;
        string? parentId = null;
        Attributes = record.Attributes.ToKeyValuePairs(options, filter: attribute =>
        {
            switch (attribute.Key)
            {
                case "{OriginalFormat}":
                    originalFormat = attribute.Value.GetString();
                    return false;
                case "ParentId":
                    parentId = attribute.Value.GetString();
                    return false;
                case "SpanId":
                case "TraceId":
                    // Explicitly ignore these
                    return false;
                default:
                    return true;
            }
        });

        TimeStamp = OtlpHelpers.UnixNanoSecondsToDateTime(record.TimeUnixNano);
        Flags = record.Flags;
        Severity = MapSeverity(record.SeverityNumber);

        Message = OtlpHelpers.TruncateString(record.Body.GetString(), options.AttributeLengthLimit);
        OriginalFormat = originalFormat;
        SpanId = record.SpanId.ToHexString();
        TraceId = record.TraceId.ToHexString();
        ParentId = parentId ?? string.Empty;
        Application = logApp;
        Scope = scope;
    }

    private static LogLevel MapSeverity(SeverityNumber severityNumber) => severityNumber switch
    {
        SeverityNumber.Trace => LogLevel.Trace,
        SeverityNumber.Trace2 => LogLevel.Trace,
        SeverityNumber.Trace3 => LogLevel.Trace,
        SeverityNumber.Trace4 => LogLevel.Trace,
        SeverityNumber.Debug => LogLevel.Debug,
        SeverityNumber.Debug2 => LogLevel.Debug,
        SeverityNumber.Debug3 => LogLevel.Debug,
        SeverityNumber.Debug4 => LogLevel.Debug,
        SeverityNumber.Info => LogLevel.Information,
        SeverityNumber.Info2 => LogLevel.Information,
        SeverityNumber.Info3 => LogLevel.Information,
        SeverityNumber.Info4 => LogLevel.Information,
        SeverityNumber.Warn => LogLevel.Warning,
        SeverityNumber.Warn2 => LogLevel.Warning,
        SeverityNumber.Warn3 => LogLevel.Warning,
        SeverityNumber.Warn4 => LogLevel.Warning,
        SeverityNumber.Error => LogLevel.Error,
        SeverityNumber.Error2 => LogLevel.Error,
        SeverityNumber.Error3 => LogLevel.Error,
        SeverityNumber.Error4 => LogLevel.Error,
        SeverityNumber.Fatal => LogLevel.Critical,
        SeverityNumber.Fatal2 => LogLevel.Critical,
        SeverityNumber.Fatal3 => LogLevel.Critical,
        SeverityNumber.Fatal4 => LogLevel.Critical,
        _ => LogLevel.None
    };
}
