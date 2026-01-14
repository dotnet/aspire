// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Helper methods for exporting telemetry data.
/// </summary>
internal static class TelemetryExportHelpers
{
    /// <summary>
    /// Downloads a span as a JSON file, including associated log entries.
    /// </summary>
    /// <param name="js">The JS runtime.</param>
    /// <param name="span">The span to download.</param>
    /// <param name="telemetryRepository">The telemetry repository to fetch logs from.</param>
    public static Task DownloadSpanAsJsonAsync(IJSRuntime js, OtlpSpan span, TelemetryRepository telemetryRepository)
    {
        var logs = telemetryRepository.GetLogsForSpan(span.TraceId, span.SpanId);
        var spanJson = TelemetryExportService.ConvertSpanToJson(span, logs);
        var fileName = $"span-{OtlpHelpers.ToShortenedId(span.SpanId)}.json";
        return js.DownloadFileAsync(fileName, spanJson);
    }

    /// <summary>
    /// Downloads a log entry as a JSON file.
    /// </summary>
    /// <param name="js">The JS runtime.</param>
    /// <param name="logEntry">The log entry to download.</param>
    public static Task DownloadLogEntryAsJsonAsync(IJSRuntime js, OtlpLogEntry logEntry)
    {
        var logJson = TelemetryExportService.ConvertLogEntryToJson(logEntry);
        var fileName = $"log-{logEntry.InternalId}.json";
        return js.DownloadFileAsync(fileName, logJson);
    }

    /// <summary>
    /// Downloads all spans in a trace as a JSON file, including associated log entries.
    /// </summary>
    /// <param name="js">The JS runtime.</param>
    /// <param name="trace">The trace to download.</param>
    /// <param name="telemetryRepository">The telemetry repository to fetch logs from.</param>
    public static Task DownloadTraceAsJsonAsync(IJSRuntime js, OtlpTrace trace, TelemetryRepository telemetryRepository)
    {
        var logs = telemetryRepository.GetLogsForTrace(trace.TraceId);
        var traceJson = TelemetryExportService.ConvertTraceToJson(trace, logs);
        var fileName = $"trace-{OtlpHelpers.ToShortenedId(trace.TraceId)}.json";
        return js.DownloadFileAsync(fileName, traceJson);
    }
}
