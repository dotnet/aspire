// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Aspire.Shared;
using Spectre.Console;

namespace Aspire.Cli.Utils;

/// <summary>
/// Formats telemetry data for human-readable console output using Spectre.Console.
/// </summary>
internal sealed class TelemetryOutputFormatter
{
    private readonly IAnsiConsole _console;
    private readonly bool _enableColor;

    private const string SuccessSymbol = "✓";
    private const string ErrorSymbol = "✗";
    private const string InfoSymbol = "•";

    public TelemetryOutputFormatter(IAnsiConsole console, bool enableColor = true)
    {
        _console = console;
        _enableColor = enableColor;
    }

    /// <summary>
    /// Formats a list of traces for human-readable console output.
    /// Traces are displayed newest first.
    /// </summary>
    /// <param name="tracesJson">JSON array of trace objects from MCP tool response.</param>
    public void FormatTraces(string tracesJson)
    {
        if (string.IsNullOrWhiteSpace(tracesJson))
        {
            WriteEmptyMessage("traces");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(tracesJson);
            var traces = document.RootElement;

            if (traces.ValueKind != JsonValueKind.Array)
            {
                WriteEmptyMessage("traces");
                return;
            }

            var traceList = traces.EnumerateArray().ToList();

            if (traceList.Count == 0)
            {
                WriteEmptyMessage("traces");
                return;
            }

            // Sort by timestamp descending (newest first) if timestamp is available
            traceList = traceList
                .OrderByDescending(GetTimestamp)
                .ToList();

            WriteHeader($"TRACES ({traceList.Count} total, newest first)");

            foreach (var trace in traceList)
            {
                FormatTrace(trace);
                _console.WriteLine();
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, try to display the raw content
            _console.MarkupLine("[dim]Unable to parse trace data.[/]");
        }
    }

    /// <summary>
    /// Formats a single trace with its spans for detailed view.
    /// </summary>
    /// <param name="traceJson">JSON object of a single trace from MCP tool response.</param>
    public void FormatSingleTrace(string traceJson)
    {
        if (string.IsNullOrWhiteSpace(traceJson))
        {
            WriteEmptyMessage("trace");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(traceJson);
            FormatTrace(document.RootElement, detailed: true);
        }
        catch (JsonException)
        {
            _console.MarkupLine("[dim]Unable to parse trace data.[/]");
        }
    }

    private void FormatTrace(JsonElement trace, bool detailed = false)
    {
        var traceId = GetStringProperty(trace, "trace_id") ?? "unknown";
        var title = GetStringProperty(trace, "title") ?? "Unknown";
        var durationMs = GetDoubleProperty(trace, "duration_ms");
        var hasError = GetBoolProperty(trace, "has_error");
        var timestamp = GetTimestamp(trace);

        // Format: [Status] TraceId - Title (Duration)
        var statusSymbol = hasError ? ErrorSymbol : SuccessSymbol;
        var statusColor = hasError ? "red" : "green";

        var durationFormatted = durationMs.HasValue
            ? DurationFormatter.FormatDuration(TimeSpan.FromMilliseconds(durationMs.Value), CultureInfo.InvariantCulture)
            : "?";

        var timestampFormatted = timestamp.HasValue
            ? timestamp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : "";

        if (_enableColor)
        {
            _console.MarkupLine($"[{statusColor}]{statusSymbol}[/] [cyan]{traceId.EscapeMarkup()}[/] - {title.EscapeMarkup()} [dim]({durationFormatted})[/]");
        }
        else
        {
            _console.WriteLine($"{statusSymbol} {traceId} - {title} ({durationFormatted})");
        }

        if (!string.IsNullOrEmpty(timestampFormatted))
        {
            if (_enableColor)
            {
                _console.MarkupLine($"  [dim]Time:[/] {timestampFormatted}");
            }
            else
            {
                _console.WriteLine($"  Time: {timestampFormatted}");
            }
        }

        // Show spans
        if (trace.TryGetProperty("spans", out var spans) && spans.ValueKind == JsonValueKind.Array)
        {
            var spanList = spans.EnumerateArray().ToList();

            if (!detailed && spanList.Count > 0)
            {
                // Summarize span info - show resource flow
                var resources = spanList
                    .Select(s => GetStringProperty(s, "source"))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();

                if (resources.Count > 0)
                {
                    var resourceFlow = string.Join(" → ", resources!);
                    if (_enableColor)
                    {
                        _console.MarkupLine($"  [dim]Resources:[/] {resourceFlow.EscapeMarkup()}");
                    }
                    else
                    {
                        _console.WriteLine($"  Resources: {resourceFlow}");
                    }
                }

                if (_enableColor)
                {
                    _console.MarkupLine($"  [dim]Spans:[/] {spanList.Count}");
                }
                else
                {
                    _console.WriteLine($"  Spans: {spanList.Count}");
                }
            }
            else if (detailed)
            {
                // Show all spans with hierarchy
                FormatSpans(spanList);
            }
        }

        // Show dashboard link if available
        if (trace.TryGetProperty("dashboard_link", out var dashboardLink))
        {
            var url = GetStringProperty(dashboardLink, "url");
            if (!string.IsNullOrEmpty(url))
            {
                if (_enableColor)
                {
                    _console.MarkupLine($"  [dim]Dashboard:[/] [link={url.EscapeMarkup()}]{url.EscapeMarkup()}[/]");
                }
                else
                {
                    _console.WriteLine($"  Dashboard: {url}");
                }
            }
        }
    }

    private void FormatSpans(List<JsonElement> spans)
    {
        if (_enableColor)
        {
            _console.MarkupLine($"  [dim]Spans ({spans.Count}):[/]");
        }
        else
        {
            _console.WriteLine($"  Spans ({spans.Count}):");
        }

        // Build span tree for proper hierarchy display
        var spanById = spans.ToDictionary(
            s => GetStringProperty(s, "span_id") ?? "",
            s => s);

        var rootSpans = spans
            .Where(s =>
            {
                var parentId = GetStringProperty(s, "parent_span_id");
                return string.IsNullOrEmpty(parentId) || !spanById.ContainsKey(parentId);
            })
            .ToList();

        foreach (var rootSpan in rootSpans)
        {
            FormatSpanWithChildren(rootSpan, spanById, indent: 2);
        }
    }

    private void FormatSpanWithChildren(JsonElement span, Dictionary<string, JsonElement> spanById, int indent)
    {
        var spanId = GetStringProperty(span, "span_id") ?? "";
        var name = GetStringProperty(span, "name") ?? "unknown";
        var source = GetStringProperty(span, "source");
        var destination = GetStringProperty(span, "destination");
        var durationMs = GetDoubleProperty(span, "duration_ms");
        var status = GetStringProperty(span, "status");
        var kind = GetStringProperty(span, "kind");

        var indentStr = new string(' ', indent * 2);
        var statusSymbol = status == "Error" ? ErrorSymbol : InfoSymbol;
        var statusColor = status == "Error" ? "red" : "dim";

        var durationFormatted = durationMs.HasValue
            ? DurationFormatter.FormatDuration(TimeSpan.FromMilliseconds(durationMs.Value), CultureInfo.InvariantCulture)
            : "?";

        var sourceDisplay = source ?? "";
        var arrow = !string.IsNullOrEmpty(destination) ? $" → {destination}" : "";

        if (_enableColor)
        {
            _console.MarkupLine($"{indentStr}[{statusColor}]{statusSymbol}[/] [{(status == "Error" ? "red" : "white")}]{name.EscapeMarkup()}[/] [dim]({durationFormatted})[/]");
            if (!string.IsNullOrEmpty(sourceDisplay))
            {
                _console.MarkupLine($"{indentStr}  [dim]{sourceDisplay.EscapeMarkup()}{arrow.EscapeMarkup()}[/]");
            }
        }
        else
        {
            _console.WriteLine($"{indentStr}{statusSymbol} {name} ({durationFormatted})");
            if (!string.IsNullOrEmpty(sourceDisplay))
            {
                _console.WriteLine($"{indentStr}  {sourceDisplay}{arrow}");
            }
        }

        // Show attributes if present
        if (span.TryGetProperty("attributes", out var attributes) && attributes.ValueKind == JsonValueKind.Object)
        {
            var attrList = attributes.EnumerateObject().ToList();
            if (attrList.Count > 0)
            {
                // Show first few attributes inline
                var displayCount = Math.Min(attrList.Count, 3);
                var attrStr = string.Join(", ", attrList.Take(displayCount).Select(a => $"{a.Name}={TruncateValue(a.Value.ToString())}"));
                var moreCount = attrList.Count - displayCount;

                if (_enableColor)
                {
                    var moreText = moreCount > 0 ? $" [dim](+{moreCount} more)[/]" : "";
                    _console.MarkupLine($"{indentStr}  [dim]{attrStr.EscapeMarkup()}{moreText}[/]");
                }
                else
                {
                    var moreText = moreCount > 0 ? $" (+{moreCount} more)" : "";
                    _console.WriteLine($"{indentStr}  {attrStr}{moreText}");
                }
            }
        }

        // Recursively show children
        var children = spanById.Values
            .Where(s => GetStringProperty(s, "parent_span_id") == spanId)
            .ToList();

        foreach (var child in children)
        {
            FormatSpanWithChildren(child, spanById, indent + 1);
        }
    }

    private void WriteHeader(string header)
    {
        if (_enableColor)
        {
            _console.MarkupLine($"[bold]{header}[/]");
            _console.MarkupLine(new string('─', Math.Min(header.Length + 10, 60)));
        }
        else
        {
            _console.WriteLine(header);
            _console.WriteLine(new string('-', Math.Min(header.Length + 10, 60)));
        }
    }

    private void WriteEmptyMessage(string dataType)
    {
        if (_enableColor)
        {
            _console.MarkupLine($"[dim]No {dataType} found.[/]");
        }
        else
        {
            _console.WriteLine($"No {dataType} found.");
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        }
        return null;
    }

    private static double? GetDoubleProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDouble();
            }
            else if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), out var result))
            {
                return result;
            }
        }
        return null;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue = false)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }
        return defaultValue;
    }

    private static DateTime? GetTimestamp(JsonElement element)
    {
        if (element.TryGetProperty("timestamp", out var property))
        {
            if (property.ValueKind == JsonValueKind.String && DateTime.TryParse(property.GetString(), out var result))
            {
                return result;
            }
        }
        return null;
    }

    private static string TruncateValue(string value, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Formats a list of logs for human-readable console output.
    /// Logs are displayed newest first.
    /// </summary>
    /// <param name="logsJson">JSON array of log objects from MCP tool response.</param>
    public void FormatLogs(string logsJson)
    {
        if (string.IsNullOrWhiteSpace(logsJson))
        {
            WriteEmptyMessage("logs");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(logsJson);
            var logs = document.RootElement;

            if (logs.ValueKind != JsonValueKind.Array)
            {
                WriteEmptyMessage("logs");
                return;
            }

            var logList = logs.EnumerateArray().ToList();

            if (logList.Count == 0)
            {
                WriteEmptyMessage("logs");
                return;
            }

            // Logs are already ordered by the MCP tool (newest first based on log_id)
            // but we can sort by log_id descending to ensure newest first
            logList = logList
                .OrderByDescending(GetLogId)
                .ToList();

            WriteHeader($"LOGS ({logList.Count} total, newest first)");

            foreach (var log in logList)
            {
                FormatLog(log);
                _console.WriteLine();
            }
        }
        catch (JsonException)
        {
            _console.MarkupLine("[dim]Unable to parse log data.[/]");
        }
    }

    /// <summary>
    /// Formats a single log entry for detailed view.
    /// </summary>
    /// <param name="logJson">JSON object of a single log from MCP tool response.</param>
    public void FormatSingleLog(string logJson)
    {
        if (string.IsNullOrWhiteSpace(logJson))
        {
            WriteEmptyMessage("log");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(logJson);
            FormatLog(document.RootElement, detailed: true);
        }
        catch (JsonException)
        {
            _console.MarkupLine("[dim]Unable to parse log data.[/]");
        }
    }

    private void FormatLog(JsonElement log, bool detailed = false)
    {
        var logId = GetStringProperty(log, "log_id") ?? "unknown";
        var severity = GetStringProperty(log, "severity") ?? "Information";
        var message = GetStringProperty(log, "message") ?? "";
        var resourceName = GetStringProperty(log, "resource_name");
        var traceId = GetStringProperty(log, "trace_id");
        var spanId = GetStringProperty(log, "span_id");
        var source = GetStringProperty(log, "source");
        var exception = GetStringProperty(log, "exception");

        // Get severity styling
        var (severitySymbol, severityColor) = GetSeverityStyle(severity);

        // Format: [SeveritySymbol] [Severity] ResourceName - Message
        if (_enableColor)
        {
            var resourceDisplay = !string.IsNullOrEmpty(resourceName) ? $"[cyan]{resourceName.EscapeMarkup()}[/] " : "";
            var messageDisplay = !string.IsNullOrEmpty(message) ? TruncateValue(message, detailed ? 500 : 100).EscapeMarkup() : "[dim](no message)[/]";
            _console.MarkupLine($"[{severityColor}]{severitySymbol}[/] [{severityColor}]{severity}[/] {resourceDisplay}{messageDisplay}");
        }
        else
        {
            var resourceDisplay = !string.IsNullOrEmpty(resourceName) ? $"{resourceName} " : "";
            var messageDisplay = !string.IsNullOrEmpty(message) ? TruncateValue(message, detailed ? 500 : 100) : "(no message)";
            _console.WriteLine($"{severitySymbol} {severity} {resourceDisplay}{messageDisplay}");
        }

        // Show log ID
        if (_enableColor)
        {
            _console.MarkupLine($"  [dim]Log ID:[/] {logId}");
        }
        else
        {
            _console.WriteLine($"  Log ID: {logId}");
        }

        // Show source/category if available
        if (!string.IsNullOrEmpty(source))
        {
            if (_enableColor)
            {
                _console.MarkupLine($"  [dim]Category:[/] {source.EscapeMarkup()}");
            }
            else
            {
                _console.WriteLine($"  Category: {source}");
            }
        }

        // Show trace/span IDs if present
        if (!string.IsNullOrEmpty(traceId) && traceId != "0000000000000000")
        {
            if (_enableColor)
            {
                _console.MarkupLine($"  [dim]Trace:[/] {traceId.EscapeMarkup()}" + (!string.IsNullOrEmpty(spanId) && spanId != "0000000000000000" ? $" [dim]Span:[/] {spanId.EscapeMarkup()}" : ""));
            }
            else
            {
                _console.WriteLine($"  Trace: {traceId}" + (!string.IsNullOrEmpty(spanId) && spanId != "0000000000000000" ? $" Span: {spanId}" : ""));
            }
        }

        // Show attributes if present
        if (log.TryGetProperty("attributes", out var attributes) && attributes.ValueKind == JsonValueKind.Object)
        {
            var attrList = attributes.EnumerateObject().ToList();
            if (attrList.Count > 0)
            {
                var displayCount = detailed ? attrList.Count : Math.Min(attrList.Count, 3);
                var attrStr = string.Join(", ", attrList.Take(displayCount).Select(a => $"{a.Name}={TruncateValue(a.Value.ToString())}"));
                var moreCount = attrList.Count - displayCount;

                if (_enableColor)
                {
                    var moreText = moreCount > 0 ? $" [dim](+{moreCount} more)[/]" : "";
                    _console.MarkupLine($"  [dim]Attributes:[/] {attrStr.EscapeMarkup()}{moreText}");
                }
                else
                {
                    var moreText = moreCount > 0 ? $" (+{moreCount} more)" : "";
                    _console.WriteLine($"  Attributes: {attrStr}{moreText}");
                }
            }
        }

        // Show exception if present
        if (!string.IsNullOrEmpty(exception))
        {
            if (_enableColor)
            {
                _console.MarkupLine($"  [red]Exception:[/]");
                // Indent exception lines
                var exceptionLines = exception.Split('\n');
                var linesToShow = detailed ? exceptionLines.Length : Math.Min(exceptionLines.Length, 5);
                for (var i = 0; i < linesToShow; i++)
                {
                    _console.MarkupLine($"    [red]{TruncateValue(exceptionLines[i], 200).EscapeMarkup()}[/]");
                }
                if (exceptionLines.Length > linesToShow)
                {
                    _console.MarkupLine($"    [dim]... ({exceptionLines.Length - linesToShow} more lines)[/]");
                }
            }
            else
            {
                _console.WriteLine("  Exception:");
                var exceptionLines = exception.Split('\n');
                var linesToShow = detailed ? exceptionLines.Length : Math.Min(exceptionLines.Length, 5);
                for (var i = 0; i < linesToShow; i++)
                {
                    _console.WriteLine($"    {TruncateValue(exceptionLines[i], 200)}");
                }
                if (exceptionLines.Length > linesToShow)
                {
                    _console.WriteLine($"    ... ({exceptionLines.Length - linesToShow} more lines)");
                }
            }
        }

        // Show dashboard link if available
        if (log.TryGetProperty("dashboard_link", out var dashboardLink))
        {
            var url = GetStringProperty(dashboardLink, "url");
            if (!string.IsNullOrEmpty(url))
            {
                if (_enableColor)
                {
                    _console.MarkupLine($"  [dim]Dashboard:[/] [link={url.EscapeMarkup()}]{url.EscapeMarkup()}[/]");
                }
                else
                {
                    _console.WriteLine($"  Dashboard: {url}");
                }
            }
        }
    }

    private static (string symbol, string color) GetSeverityStyle(string severity)
    {
        return severity.ToUpperInvariant() switch
        {
            "CRITICAL" => ("!", "red bold"),
            "ERROR" => (ErrorSymbol, "red"),
            "WARNING" => ("!", "yellow"),
            "INFORMATION" or "INFO" => (InfoSymbol, "blue"),
            "DEBUG" => (InfoSymbol, "dim"),
            "TRACE" => (InfoSymbol, "dim"),
            _ => (InfoSymbol, "white")
        };
    }

    private static int GetLogId(JsonElement element)
    {
        if (element.TryGetProperty("log_id", out var property))
        {
            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt32();
            }
            else if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var result))
            {
                return result;
            }
        }
        return 0;
    }

    /// <summary>
    /// Formats a list of metrics/instruments grouped by meter for human-readable console output.
    /// </summary>
    /// <param name="metricsJson">JSON object from list_metrics MCP tool response containing meters and instruments.</param>
    public void FormatMetricsList(string metricsJson)
    {
        if (string.IsNullOrWhiteSpace(metricsJson))
        {
            WriteEmptyMessage("metrics");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(metricsJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                WriteEmptyMessage("metrics");
                return;
            }

            var resourceName = GetStringProperty(root, "resource") ?? "unknown";
            var totalInstruments = (int?)GetDoubleProperty(root, "total_instruments") ?? 0;

            if (totalInstruments == 0)
            {
                WriteEmptyMessage("metrics");
                return;
            }

            WriteHeader($"METRICS FOR {resourceName.ToUpperInvariant()} ({totalInstruments} total)");

            if (root.TryGetProperty("meters", out var meters) && meters.ValueKind == JsonValueKind.Array)
            {
                foreach (var meter in meters.EnumerateArray())
                {
                    FormatMeter(meter);
                    _console.WriteLine();
                }
            }
        }
        catch (JsonException)
        {
            _console.MarkupLine("[dim]Unable to parse metrics data.[/]");
        }
    }

    private void FormatMeter(JsonElement meter)
    {
        var meterName = GetStringProperty(meter, "meter_name") ?? "unknown";

        if (_enableColor)
        {
            _console.MarkupLine($"[cyan bold]{meterName.EscapeMarkup()}[/]");
        }
        else
        {
            _console.WriteLine(meterName);
        }

        if (meter.TryGetProperty("instruments", out var instruments) && instruments.ValueKind == JsonValueKind.Array)
        {
            foreach (var instrument in instruments.EnumerateArray())
            {
                FormatInstrumentSummary(instrument);
            }
        }
    }

    private void FormatInstrumentSummary(JsonElement instrument)
    {
        var name = GetStringProperty(instrument, "name") ?? "unknown";
        var description = GetStringProperty(instrument, "description") ?? "";
        var unit = GetStringProperty(instrument, "unit") ?? "";
        var type = GetStringProperty(instrument, "type") ?? "";

        var unitDisplay = !string.IsNullOrEmpty(unit) ? $" [{unit}]" : "";
        var typeDisplay = !string.IsNullOrEmpty(type) ? $"({type})" : "";

        if (_enableColor)
        {
            _console.MarkupLine($"  {InfoSymbol} [white]{name.EscapeMarkup()}[/]{unitDisplay.EscapeMarkup()} [dim]{typeDisplay.EscapeMarkup()}[/]");
            if (!string.IsNullOrEmpty(description))
            {
                _console.MarkupLine($"    [dim]{TruncateValue(description, 80).EscapeMarkup()}[/]");
            }
        }
        else
        {
            _console.WriteLine($"  {InfoSymbol} {name}{unitDisplay} {typeDisplay}");
            if (!string.IsNullOrEmpty(description))
            {
                _console.WriteLine($"    {TruncateValue(description, 80)}");
            }
        }
    }

    /// <summary>
    /// Formats metric data for a specific instrument for human-readable console output.
    /// </summary>
    /// <param name="metricDataJson">JSON object from get_metric_data MCP tool response.</param>
    public void FormatMetricData(string metricDataJson)
    {
        if (string.IsNullOrWhiteSpace(metricDataJson))
        {
            WriteEmptyMessage("metric data");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(metricDataJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                WriteEmptyMessage("metric data");
                return;
            }

            var resourceName = GetStringProperty(root, "resource") ?? "unknown";
            var meterName = GetStringProperty(root, "meter") ?? "unknown";

            // Get instrument details
            string instrumentName = "unknown", instrumentUnit = "", instrumentType = "", instrumentDescription = "";
            if (root.TryGetProperty("instrument", out var instrument))
            {
                instrumentName = GetStringProperty(instrument, "name") ?? "unknown";
                instrumentDescription = GetStringProperty(instrument, "description") ?? "";
                instrumentUnit = GetStringProperty(instrument, "unit") ?? "";
                instrumentType = GetStringProperty(instrument, "type") ?? "";
            }

            // Get time window
            string duration = "5m";
            if (root.TryGetProperty("time_window", out var timeWindow))
            {
                duration = GetStringProperty(timeWindow, "duration") ?? "5m";
            }

            var unitDisplay = !string.IsNullOrEmpty(instrumentUnit) ? $" [{instrumentUnit}]" : "";
            WriteHeader($"METRIC: {instrumentName}{unitDisplay} (last {duration})");

            // Show instrument summary
            if (_enableColor)
            {
                _console.MarkupLine($"[dim]Resource:[/] {resourceName.EscapeMarkup()}");
                _console.MarkupLine($"[dim]Meter:[/] {meterName.EscapeMarkup()}");
                _console.MarkupLine($"[dim]Type:[/] {instrumentType.EscapeMarkup()}");
                if (!string.IsNullOrEmpty(instrumentDescription))
                {
                    _console.MarkupLine($"[dim]Description:[/] {instrumentDescription.EscapeMarkup()}");
                }
            }
            else
            {
                _console.WriteLine($"Resource: {resourceName}");
                _console.WriteLine($"Meter: {meterName}");
                _console.WriteLine($"Type: {instrumentType}");
                if (!string.IsNullOrEmpty(instrumentDescription))
                {
                    _console.WriteLine($"Description: {instrumentDescription}");
                }
            }
            _console.WriteLine();

            // Show dimensions
            var dimensionCount = (int?)GetDoubleProperty(root, "dimension_count") ?? 0;
            if (root.TryGetProperty("dimensions", out var dimensions) && dimensions.ValueKind == JsonValueKind.Array)
            {
                var dimensionList = dimensions.EnumerateArray().ToList();

                if (dimensionList.Count == 0)
                {
                    if (_enableColor)
                    {
                        _console.MarkupLine("[dim]No dimension data available.[/]");
                    }
                    else
                    {
                        _console.WriteLine("No dimension data available.");
                    }
                }
                else
                {
                    if (_enableColor)
                    {
                        _console.MarkupLine($"[bold]Dimensions ({dimensionList.Count}):[/]");
                    }
                    else
                    {
                        _console.WriteLine($"Dimensions ({dimensionList.Count}):");
                    }

                    foreach (var dimension in dimensionList)
                    {
                        FormatDimension(dimension, instrumentUnit);
                    }
                }
            }
        }
        catch (JsonException)
        {
            _console.MarkupLine("[dim]Unable to parse metric data.[/]");
        }
    }

    private void FormatDimension(JsonElement dimension, string unit)
    {
        var name = GetStringProperty(dimension, "name") ?? "";
        var valueCount = (int?)GetDoubleProperty(dimension, "value_count") ?? 0;

        // Format attributes as key=value pairs
        var attributeDisplay = "";
        if (dimension.TryGetProperty("attributes", out var attributes) && attributes.ValueKind == JsonValueKind.Object)
        {
            var attrList = attributes.EnumerateObject().ToList();
            if (attrList.Count > 0)
            {
                attributeDisplay = string.Join(", ", attrList.Select(a => $"{a.Name}={TruncateValue(a.Value.ToString(), 30)}"));
            }
        }

        // Get latest value for display
        string latestValueDisplay = "";
        if (dimension.TryGetProperty("latest_values", out var latestValues) && latestValues.ValueKind == JsonValueKind.Array)
        {
            var valuesList = latestValues.EnumerateArray().ToList();
            if (valuesList.Count > 0)
            {
                var lastValue = valuesList[^1];
                latestValueDisplay = FormatMetricValueForDisplay(lastValue, unit);
            }
        }

        if (_enableColor)
        {
            var attrPart = !string.IsNullOrEmpty(attributeDisplay) ? $" [dim]({attributeDisplay.EscapeMarkup()})[/]" : " [dim](no attributes)[/]";
            var valuePart = !string.IsNullOrEmpty(latestValueDisplay) ? $" = [yellow]{latestValueDisplay.EscapeMarkup()}[/]" : "";
            _console.MarkupLine($"  {InfoSymbol}{attrPart}{valuePart}");
            _console.MarkupLine($"    [dim]{valueCount} data points[/]");
        }
        else
        {
            var attrPart = !string.IsNullOrEmpty(attributeDisplay) ? $" ({attributeDisplay})" : " (no attributes)";
            var valuePart = !string.IsNullOrEmpty(latestValueDisplay) ? $" = {latestValueDisplay}" : "";
            _console.WriteLine($"  {InfoSymbol}{attrPart}{valuePart}");
            _console.WriteLine($"    {valueCount} data points");
        }
    }

    private static string FormatMetricValueForDisplay(JsonElement valueElement, string unit)
    {
        if (!valueElement.TryGetProperty("value", out var value))
        {
            return "";
        }

        // Handle histogram values
        if (value.ValueKind == JsonValueKind.Object)
        {
            var count = GetDoubleProperty(value, "count");
            var sum = GetDoubleProperty(value, "sum");
            if (count.HasValue && sum.HasValue)
            {
                var avg = count.Value > 0 ? sum.Value / count.Value : 0;
                return $"count={count.Value:N0}, avg={FormatValueWithUnit(avg, unit)}";
            }
            return "histogram";
        }

        // Handle numeric values
        if (value.ValueKind == JsonValueKind.Number)
        {
            var numericValue = value.GetDouble();
            return FormatValueWithUnit(numericValue, unit);
        }

        return value.ToString();
    }

    private static string FormatValueWithUnit(double value, string unit)
    {
        // Format bytes nicely
        var lowerUnit = unit.ToLowerInvariant();
        if (lowerUnit == "by" || lowerUnit == "bytes" || lowerUnit.Contains("byte"))
        {
            return FormatBytes(value);
        }

        // Format durations nicely (seconds, milliseconds)
        if (lowerUnit == "s" || lowerUnit == "seconds" || lowerUnit == "sec")
        {
            if (value >= 1)
            {
                return $"{value:F2}s";
            }
            return $"{value * 1000:F2}ms";
        }

        if (lowerUnit == "ms" || lowerUnit == "milliseconds")
        {
            return $"{value:F2}ms";
        }

        // Format percentages
        if (lowerUnit == "%" || lowerUnit == "percent")
        {
            return $"{value:F1}%";
        }

        // Default formatting
        if (value >= 1000000)
        {
            return $"{value / 1000000:F2}M";
        }
        if (value >= 1000)
        {
            return $"{value / 1000:F2}K";
        }
        if (value == Math.Floor(value))
        {
            return $"{value:F0}";
        }
        return $"{value:F2}";
    }

    private static string FormatBytes(double bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes /= 1024;
        }
        return $"{bytes:F2} {sizes[order]}";
    }
}
