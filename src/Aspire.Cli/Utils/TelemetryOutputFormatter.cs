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
}
