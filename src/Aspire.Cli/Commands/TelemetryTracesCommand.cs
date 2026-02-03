// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Otlp;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command to view traces from the Dashboard telemetry API.
/// </summary>
internal sealed class TelemetryTracesCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly ILogger<TelemetryTracesCommand> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // Shared options from TelemetryCommandHelpers
    private static readonly Argument<string?> s_resourceArgument = TelemetryCommandHelpers.CreateResourceArgument();
    private static readonly Option<FileInfo?> s_projectOption = TelemetryCommandHelpers.CreateProjectOption();
    private static readonly Option<OutputFormat> s_formatOption = TelemetryCommandHelpers.CreateFormatOption();
    private static readonly Option<int?> s_limitOption = TelemetryCommandHelpers.CreateLimitOption();
    private static readonly Option<string?> s_traceIdOption = TelemetryCommandHelpers.CreateTraceIdOption("--trace-id", "-t");
    private static readonly Option<bool?> s_hasErrorOption = TelemetryCommandHelpers.CreateHasErrorOption();

    public TelemetryTracesCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        IHttpClientFactory httpClientFactory,
        ILogger<TelemetryTracesCommand> logger)
        : base("traces", TelemetryCommandStrings.TracesDescription, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);

        Arguments.Add(s_resourceArgument);
        Options.Add(s_projectOption);
        Options.Add(s_formatOption);
        Options.Add(s_limitOption);
        Options.Add(s_traceIdOption);
        Options.Add(s_hasErrorOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var resourceName = parseResult.GetValue(s_resourceArgument);
        var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);
        var format = parseResult.GetValue(s_formatOption);
        var limit = parseResult.GetValue(s_limitOption);
        var traceId = parseResult.GetValue(s_traceIdOption);
        var hasError = parseResult.GetValue(s_hasErrorOption);

        // Validate --limit value
        if (limit.HasValue && limit.Value < 1)
        {
            _interactionService.DisplayError(TelemetryCommandStrings.LimitMustBePositive);
            return ExitCodeConstants.InvalidCommand;
        }

        var (success, baseUrl, apiToken, _, exitCode) = await TelemetryCommandHelpers.GetDashboardApiAsync(
            _connectionResolver, _interactionService, passedAppHostProjectFile, format, cancellationToken);

        if (!success)
        {
            return exitCode;
        }

        if (!string.IsNullOrEmpty(traceId))
        {
            return await FetchSingleTraceAsync(baseUrl!, apiToken!, traceId, format, cancellationToken);
        }
        else
        {
            return await FetchTracesAsync(baseUrl!, apiToken!, resourceName, hasError, limit, format, cancellationToken);
        }
    }

    private async Task<int> FetchSingleTraceAsync(
        string baseUrl,
        string apiToken,
        string traceId,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        using var client = TelemetryCommandHelpers.CreateApiClient(_httpClientFactory, apiToken);

        var url = DashboardUrls.TelemetryTraceDetailApiUrl(baseUrl, traceId);

        _logger.LogDebug("Fetching trace {TraceId} from {Url}", traceId, url);

        try
        {
            var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TelemetryCommandStrings.TraceNotFound, traceId));
                return ExitCodeConstants.InvalidCommand;
            }

            response.EnsureSuccessStatusCode();

            if (!TelemetryCommandHelpers.HasJsonContentType(response))
            {
                _interactionService.DisplayError(TelemetryCommandStrings.UnexpectedContentType);
                return ExitCodeConstants.DashboardFailure;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (format == OutputFormat.Json)
            {
                _interactionService.DisplayRawText(json);
            }
            else
            {
                DisplayTraceDetails(json, traceId);
            }

            return ExitCodeConstants.Success;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch trace from Dashboard API");
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TelemetryCommandStrings.FailedToFetchTelemetry, ex.Message));
            return ExitCodeConstants.DashboardFailure;
        }
    }

    private async Task<int> FetchTracesAsync(
        string baseUrl,
        string apiToken,
        string? resource,
        bool? hasError,
        int? limit,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        using var client = TelemetryCommandHelpers.CreateApiClient(_httpClientFactory, apiToken);

        // Resolve resource name to specific instances (handles replicas)
        var resolvedResources = await TelemetryCommandHelpers.ResolveResourceNamesAsync(
            client, baseUrl, resource, cancellationToken);

        // If a resource was specified but not found, show error
        if (!string.IsNullOrEmpty(resource) && resolvedResources?.Count == 0)
        {
            _interactionService.DisplayError($"Resource '{resource}' not found.");
            return ExitCodeConstants.InvalidCommand;
        }

        // Build query string with multiple resource parameters
        var additionalParams = new List<(string key, string? value)>();
        if (hasError.HasValue)
        {
            additionalParams.Add(("hasError", hasError.Value.ToString().ToLowerInvariant()));
        }
        if (limit.HasValue)
        {
            additionalParams.Add(("limit", limit.Value.ToString(CultureInfo.InvariantCulture)));
        }

        var url = DashboardUrls.TelemetryTracesApiUrl(baseUrl, resolvedResources, [.. additionalParams]);

        _logger.LogDebug("Fetching traces from {Url}", url);

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (!TelemetryCommandHelpers.HasJsonContentType(response))
            {
                _interactionService.DisplayError(TelemetryCommandStrings.UnexpectedContentType);
                return ExitCodeConstants.DashboardFailure;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (format == OutputFormat.Json)
            {
                _interactionService.DisplayRawText(json);
            }
            else
            {
                DisplayTracesTable(json);
            }

            return ExitCodeConstants.Success;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch traces from Dashboard API");
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TelemetryCommandStrings.FailedToFetchTelemetry, ex.Message));
            return ExitCodeConstants.DashboardFailure;
        }
    }

    private static void DisplayTracesTable(string json)
    {
        var response = JsonSerializer.Deserialize(json, OtlpCliJsonSerializerContext.Default.TelemetryApiResponse);
        var resourceSpans = response?.Data?.ResourceSpans;

        if (resourceSpans is null or { Length: 0 })
        {
            TelemetryCommandHelpers.DisplayNoData("traces");
            return;
        }

        var table = new Table();
        table.AddColumn("Trace ID");
        table.AddColumn("Resource");
        table.AddColumn("Duration");
        table.AddColumn("Spans");
        table.AddColumn("Status");

        // Group by traceId to show trace summary
        var traceInfos = new Dictionary<string, (string Resource, TimeSpan Duration, int SpanCount, bool HasError)>();

        foreach (var resourceSpan in resourceSpans)
        {
            var resourceName = resourceSpan.Resource?.GetServiceName() ?? "unknown";

            foreach (var scopeSpan in resourceSpan.ScopeSpans ?? [])
            {
                foreach (var span in scopeSpan.Spans ?? [])
                {
                    var traceIdValue = span.TraceId ?? "";

                    if (string.IsNullOrEmpty(traceIdValue))
                    {
                        continue;
                    }

                    var duration = OtlpHelpers.CalculateDuration(span.StartTimeUnixNano, span.EndTimeUnixNano);
                    var hasError = span.Status?.Code == 2; // ERROR status

                    if (traceInfos.TryGetValue(traceIdValue, out var info))
                    {
                        var maxDuration = info.Duration > duration ? info.Duration : duration;
                        traceInfos[traceIdValue] = (info.Resource, maxDuration, info.SpanCount + 1, info.HasError || hasError);
                    }
                    else
                    {
                        traceInfos[traceIdValue] = (resourceName, duration, 1, hasError);
                    }
                }
            }
        }

        foreach (var (traceIdKey, info) in traceInfos.OrderByDescending(x => x.Value.Duration))
        {
            var statusText = info.HasError ? "[red]ERR[/]" : "[green]OK[/]";
            var durationStr = TelemetryCommandHelpers.FormatDuration(info.Duration);
            table.AddRow(traceIdKey, info.Resource, durationStr, info.SpanCount.ToString(CultureInfo.InvariantCulture), statusText);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]Showing {traceInfos.Count} of {response?.TotalCount ?? traceInfos.Count} traces[/]");
    }

    private static void DisplayTraceDetails(string json, string traceId)
    {
        var response = JsonSerializer.Deserialize(json, OtlpCliJsonSerializerContext.Default.TelemetryApiResponse);
        var resourceSpans = response?.Data?.ResourceSpans;

        // Collect all spans with their metadata
        var spans = new List<SpanInfo>();

        foreach (var resourceSpan in resourceSpans ?? [])
        {
            var resourceName = resourceSpan.Resource?.GetServiceName() ?? "unknown";

            foreach (var scopeSpan in resourceSpan.ScopeSpans ?? [])
            {
                foreach (var span in scopeSpan.Spans ?? [])
                {
                    var spanId = span.SpanId ?? "";
                    var parentSpanId = span.ParentSpanId;
                    var name = span.Name ?? "";
                    var duration = OtlpHelpers.CalculateDuration(span.StartTimeUnixNano, span.EndTimeUnixNano);
                    var startNano = (long)(span.StartTimeUnixNano ?? 0);
                    var hasError = span.Status?.Code == 2; // ERROR status

                    spans.Add(new SpanInfo(spanId, parentSpanId, resourceName, name, duration, startNano, hasError));
                }
            }
        }

        if (spans.Count == 0)
        {
            AnsiConsole.MarkupLine($"[bold]Trace: {traceId}[/]");
            AnsiConsole.MarkupLine("[dim]No spans found[/]");
            return;
        }

        // Calculate total duration from root spans
        var rootSpans = spans.Where(s => string.IsNullOrEmpty(s.ParentSpanId)).ToList();
        var totalDuration = rootSpans.Count > 0 ? rootSpans.Max(s => s.Duration) : spans.Max(s => s.Duration);

        // Header
        AnsiConsole.MarkupLine($"[bold]Trace:[/] {traceId}");
        AnsiConsole.MarkupLine($"[bold]Duration:[/] {TelemetryCommandHelpers.FormatDuration(totalDuration)}  [bold]Spans:[/] {spans.Count}");
        AnsiConsole.WriteLine();

        // Build tree and display
        DisplaySpanTree(spans);
    }

    private static void DisplaySpanTree(List<SpanInfo> spans)
    {
        // Build a lookup of children by parent ID
        var childrenByParent = spans
            .Where(s => !string.IsNullOrEmpty(s.ParentSpanId))
            .GroupBy(s => s.ParentSpanId!)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartNano).ToList());

        // Find root spans (no parent or parent not in this trace)
        var spanIds = spans.Select(s => s.SpanId).ToHashSet();
        var rootSpans = spans
            .Where(s => string.IsNullOrEmpty(s.ParentSpanId) || !spanIds.Contains(s.ParentSpanId!))
            .OrderBy(s => s.StartNano)
            .ToList();

        // Track which resources we've seen to show resource transitions
        string? lastResource = null;

        foreach (var root in rootSpans)
        {
            DisplaySpanNode(root, childrenByParent, "", true, ref lastResource);
        }
    }

    private static void DisplaySpanNode(
        SpanInfo span,
        Dictionary<string, List<SpanInfo>> childrenByParent,
        string indent,
        bool isLast,
        ref string? lastResource)
    {
        // Show resource name when it changes (indicates cross-service call)
        if (span.ResourceName != lastResource)
        {
            if (lastResource != null)
            {
                AnsiConsole.WriteLine(); // Blank line between resources
            }
            AnsiConsole.MarkupLine($"{indent}[bold blue]{span.ResourceName.EscapeMarkup()}[/]");
            lastResource = span.ResourceName;
        }

        // Build the connector
        var connector = isLast ? "└─" : "├─";
        var childIndent = indent + (isLast ? "   " : "│  ");

        // Format span line with spanId
        var statusColor = span.HasError ? "red" : "green";
        var statusText = span.HasError ? "ERR" : "OK";
        var durationStr = TelemetryCommandHelpers.FormatDuration(span.Duration).PadLeft(8);
        var shortenedSpanId = OtlpHelpers.ToShortenedId(span.SpanId);
        var escapedName = span.Name.EscapeMarkup();

        // Truncate long names
        var maxNameLength = 50;
        var displayName = escapedName.Length > maxNameLength
            ? escapedName[..(maxNameLength - 3)] + "..."
            : escapedName;

        AnsiConsole.MarkupLine($"{indent}{connector} [dim]{shortenedSpanId}[/] {displayName} [{statusColor}]{statusText}[/] [dim]{durationStr}[/]");

        // Render children
        if (childrenByParent.TryGetValue(span.SpanId, out var children))
        {
            for (var i = 0; i < children.Count; i++)
            {
                DisplaySpanNode(children[i], childrenByParent, childIndent, i == children.Count - 1, ref lastResource);
            }
        }
    }

    private sealed record SpanInfo(
        string SpanId,
        string? ParentSpanId,
        string ResourceName,
        string Name,
        TimeSpan Duration,
        long StartNano,
        bool HasError);
}
