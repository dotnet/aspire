// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry.Proto.Logs.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("TimeStamp = {TimeStamp}, Severity = {Severity}, Message = {Message}")]
public class OtlpLogEntry
{
    public KeyValuePair<string, string>[] Properties { get; }
    public DateTime TimeStamp { get; }
    public uint Flags { get; }
    public LogLevel Severity { get; }
    public string Message { get; }
    public string SpanId { get; }
    public string TraceId { get; }
    public string? OriginalFormat { get; }
    public OtlpApplication Application { get; }
    public OtlpScope Scope { get; }

    public OtlpLogEntry(LogRecord record, OtlpApplication logApp, OtlpScope scope)
    {
        var properties = new List<KeyValuePair<string, string>>();
        foreach (var kv in record.Attributes)
        {
            switch (kv.Key)
            {
                case "{OriginalFormat}":
                    OriginalFormat = kv.Value.GetString();
                    break;
                case "SpanId":
                case "TraceId":
                    // Explicitly ignore these
                    break;
                default:
                    properties.Add(KeyValuePair.Create(kv.Key, kv.Value.GetString()));
                    break;
            }
        }
        Properties = properties.ToArray();

        TimeStamp = OtlpHelpers.UnixNanoSecondsToDateTime(record.TimeUnixNano);
        Flags = record.Flags;
        Severity = MapSeverity(record.SeverityNumber);

        Message = record.Body.GetString();
        SpanId = record.SpanId.ToHexString();
        TraceId = record.TraceId.ToHexString();
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

    public Dictionary<string, string> AllProperties()
    {
        var props = new Dictionary<string, string>();

        AddOptionalValue("Application", Application.ApplicationName, props);
        AddOptionalValue("Category", Scope.ScopeName, props);
        AddOptionalValue("Level", Severity.ToString(), props);
        AddOptionalValue("Message", Message, props);
        AddOptionalValue("TraceId", TraceId, props);
        AddOptionalValue("SpanId", SpanId, props);
        AddOptionalValue("OriginalFormat", OriginalFormat, props);

        foreach (var kv in Properties)
        {
            props.TryAdd(kv.Key, kv.Value);
        }

        return props;

        static void AddOptionalValue(string name, string? value, Dictionary<string, string> props)
        {
            if (!string.IsNullOrEmpty(value))
            {
                props.Add(name, value);
            }
        }
    }
}
