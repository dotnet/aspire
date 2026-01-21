// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Represents the result of exporting telemetry data to JSON.
/// </summary>
/// <param name="Json">The JSON representation of the telemetry data.</param>
/// <param name="FileName">The suggested file name for downloading the JSON.</param>
internal sealed record TelemetryJsonExportResult(string Json, string FileName);

/// <summary>
/// Helper methods for exporting telemetry data.
/// </summary>
internal static class TelemetryExportHelpers
{
    /// <summary>
    /// Gets a span as a JSON export result, including associated log entries.
    /// </summary>
    /// <param name="span">The span to convert.</param>
    /// <param name="telemetryRepository">The telemetry repository to fetch logs from.</param>
    /// <returns>A result containing the JSON representation and suggested file name.</returns>
    public static TelemetryJsonExportResult GetSpanAsJson(OtlpSpan span, TelemetryRepository telemetryRepository)
    {
        var logs = telemetryRepository.GetLogsForSpan(span.TraceId, span.SpanId);
        var json = TelemetryExportService.ConvertSpanToJson(span, logs);
        var fileName = $"span-{OtlpHelpers.ToShortenedId(span.SpanId)}.json";
        return new TelemetryJsonExportResult(json, fileName);
    }

    /// <summary>
    /// Gets a log entry as a JSON export result.
    /// </summary>
    /// <param name="logEntry">The log entry to convert.</param>
    /// <returns>A result containing the JSON representation and suggested file name.</returns>
    public static TelemetryJsonExportResult GetLogEntryAsJson(OtlpLogEntry logEntry)
    {
        var json = TelemetryExportService.ConvertLogEntryToJson(logEntry);
        var fileName = $"log-{logEntry.InternalId}.json";
        return new TelemetryJsonExportResult(json, fileName);
    }

    /// <summary>
    /// Gets all spans in a trace as a JSON export result, including associated log entries.
    /// </summary>
    /// <param name="trace">The trace to convert.</param>
    /// <param name="telemetryRepository">The telemetry repository to fetch logs from.</param>
    /// <returns>A result containing the JSON representation and suggested file name.</returns>
    public static TelemetryJsonExportResult GetTraceAsJson(OtlpTrace trace, TelemetryRepository telemetryRepository)
    {
        var logs = telemetryRepository.GetLogsForTrace(trace.TraceId);
        var json = TelemetryExportService.ConvertTraceToJson(trace, logs);
        var fileName = $"trace-{OtlpHelpers.ToShortenedId(trace.TraceId)}.json";
        return new TelemetryJsonExportResult(json, fileName);
    }
}
