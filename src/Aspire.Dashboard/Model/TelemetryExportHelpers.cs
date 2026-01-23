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
/// Represents the result of exporting text data.
/// </summary>
/// <param name="Content">The text content to export.</param>
/// <param name="FileName">The suggested file name for downloading the content.</param>
internal sealed record TextExportResult(string Content, string FileName);

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

    /// <summary>
    /// Gets a resource as a JSON export result.
    /// </summary>
    /// <param name="resource">The resource to convert.</param>
    /// <param name="getResourceName">A function to resolve the resource name for the file name.</param>
    /// <returns>A result containing the JSON representation and suggested file name.</returns>
    public static TelemetryJsonExportResult GetResourceAsJson(ResourceViewModel resource, Func<ResourceViewModel, string> getResourceName)
    {
        var json = TelemetryExportService.ConvertResourceToJson(resource);
        var fileName = $"{getResourceName(resource)}.json";
        return new TelemetryJsonExportResult(json, fileName);
    }

    /// <summary>
    /// Gets environment variables as a .env file export result.
    /// </summary>
    /// <param name="resource">The resource containing environment variables.</param>
    /// <param name="getResourceName">A function to resolve the resource name for the file name.</param>
    /// <returns>A result containing the .env file content and suggested file name.</returns>
    public static TextExportResult GetEnvironmentVariablesAsEnvFile(ResourceViewModel resource, Func<ResourceViewModel, string> getResourceName)
    {
        var envContent = ConvertEnvironmentVariablesToEnvFormat(resource.Environment);
        var fileName = $"{getResourceName(resource)}.env";
        return new TextExportResult(envContent, fileName);
    }

    /// <summary>
    /// Converts environment variables to .env file format.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to convert.</param>
    /// <returns>A string in .env file format.</returns>
    private static string ConvertEnvironmentVariablesToEnvFormat(IEnumerable<EnvironmentVariableViewModel> environmentVariables)
    {
        var builder = new System.Text.StringBuilder();
        
        foreach (var envVar in environmentVariables.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            // Format: KEY=VALUE
            // Handle values that contain special characters by quoting them if needed
            var value = envVar.Value ?? string.Empty;
            
            // Quote values that contain spaces, quotes, or other special characters
            if (NeedsQuoting(value))
            {
                // Escape special characters
                value = value.Replace("\\", "\\\\")  // Backslashes first
                             .Replace("\"", "\\\"")  // Quotes
                             .Replace("\n", "\\n")   // Newlines
                             .Replace("\r", "\\r")   // Carriage returns
                             .Replace("\t", "\\t");  // Tabs
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{envVar.Name}=\"{value}\"");
            }
            else
            {
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{envVar.Name}={value}");
            }
        }
        
        return builder.ToString();
    }

    /// <summary>
    /// Determines if a value needs to be quoted in a .env file.
    /// </summary>
    private static bool NeedsQuoting(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // Quote if contains spaces, quotes, special shell characters, or starts/ends with whitespace
        return value.Contains(' ') ||
               value.Contains('"') ||
               value.Contains('\'') ||
               value.Contains('$') ||
               value.Contains('\\') ||
               value.Contains('\n') ||
               value.Contains('\r') ||
               value.Contains('\t') ||
               char.IsWhiteSpace(value[0]) ||
               char.IsWhiteSpace(value[^1]);
    }
}
