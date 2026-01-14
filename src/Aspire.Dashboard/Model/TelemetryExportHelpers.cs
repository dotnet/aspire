// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Otlp.Model;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Helper methods for exporting telemetry data.
/// </summary>
internal static class TelemetryExportHelpers
{
    /// <summary>
    /// Downloads a span as a JSON file.
    /// </summary>
    /// <param name="js">The JS runtime.</param>
    /// <param name="span">The span to download.</param>
    public static Task DownloadSpanAsJsonAsync(IJSRuntime js, OtlpSpan span)
    {
        var spanJson = TelemetryExportService.ConvertSpanToJson(span);
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
    /// Downloads all spans in a trace as a JSON file.
    /// </summary>
    /// <param name="js">The JS runtime.</param>
    /// <param name="trace">The trace to download.</param>
    public static Task DownloadTraceAsJsonAsync(IJSRuntime js, OtlpTrace trace)
    {
        var traceJson = TelemetryExportService.ConvertTraceToJson(trace);
        var fileName = $"trace-{OtlpHelpers.ToShortenedId(trace.TraceId)}.json";
        return js.DownloadFileAsync(fileName, traceJson);
    }
}
