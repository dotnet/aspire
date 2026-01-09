// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// MCP tools for telemetry data that don't require a resource service.
/// </summary>
internal sealed class AspireTelemetryMcpTools
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly IEnumerable<IOutgoingPeerResolver> _outgoingPeerResolvers;
    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;
    private readonly IDashboardClient _dashboardClient;
    private readonly ILogger<AspireTelemetryMcpTools> _logger;

    public AspireTelemetryMcpTools(TelemetryRepository telemetryRepository,
        IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers,
        IOptionsMonitor<DashboardOptions> dashboardOptions,
        IDashboardClient dashboardClient,
        ILogger<AspireTelemetryMcpTools> logger)
    {
        _telemetryRepository = telemetryRepository;
        _outgoingPeerResolvers = outgoingPeerResolvers;
        _dashboardOptions = dashboardOptions;
        _dashboardClient = dashboardClient;
        _logger = logger;
    }

    [McpServerTool(Name = "list_structured_logs")]
    [Description("List structured logs for resources.")]
    public string ListStructuredLogs(
        [Description("The resource name. This limits logs returned to the specified resource. If no resource name is specified then structured logs for all resources are returned.")]
        string? resourceName = null,
        [Description("JSON array of filter objects. Each filter object should have 'field' (string), 'condition' (string: 'equals', '!equals', 'contains', '!contains', 'gt', 'lt', 'gte', 'lte'), and 'value' (string) properties. Example: [{\"field\":\"log.category\",\"condition\":\"contains\",\"value\":\"MyApp\"}]")]
        string? filters = null,
        [Description("Minimum severity level. Logs with this severity and above will be returned. Valid values: Trace, Debug, Information, Warning, Error, Critical.")]
        string? severity = null)
    {
        _logger.LogDebug("MCP tool list_structured_logs called with resource '{ResourceName}', filters '{Filters}', severity '{Severity}'.", resourceName, filters, severity);

        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            return message;
        }

        List<TelemetryFilter> telemetryFilters = [];
        if (!string.IsNullOrWhiteSpace(filters))
        {
            if (!TryParseFilters(filters, out var parsedFilters, out var filterError))
            {
                return filterError;
            }
            telemetryFilters = parsedFilters;
        }

        // Add severity filter if provided
        if (!string.IsNullOrWhiteSpace(severity))
        {
            if (!TryParseSeverity(severity, out var parsedSeverity, out var severityError))
            {
                return severityError;
            }

            // Only add filter if severity is above Trace (Trace returns all)
            if (parsedSeverity != LogLevel.Trace)
            {
                telemetryFilters.Add(new FieldTelemetryFilter
                {
                    Field = nameof(OtlpLogEntry.Severity),
                    Condition = FilterCondition.GreaterThanOrEqual,
                    Value = parsedSeverity.ToString()
                });
            }
        }

        // Get all logs because we want the most recent logs and they're at the end of the results.
        // If support is added for ordering logs by timestamp then improve this.
        var logs = _telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = telemetryFilters
        }).Items;

        if (_dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(_dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                logs = logs.Where(l => !optOutResources.Any(r => l.ResourceView.ResourceKey.EqualsCompositeName(r.Name))).ToList();
            }
        }

        var resources = _telemetryRepository.GetResources();

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(
            logs,
            _dashboardOptions.CurrentValue,
            includeDashboardUrl: true,
            getResourceName: r => OtlpResource.GetResourceName(r, resources));

        var response = $"""
            Always format log_id in the response as code like this: `log_id: 123`.
            {limitMessage}

            # STRUCTURED LOGS DATA

            {logsData}
            """;

        return response;
    }

    [McpServerTool(Name = "list_traces")]
    [Description("List distributed traces for resources. A distributed trace is used to track operations. A distributed trace can span multiple resources across a distributed system. Includes a list of distributed traces with their IDs, resources in the trace, duration and whether an error occurred in the trace.")]
    public string ListTraces(
        [Description("The resource name. This limits traces returned to the specified resource. If no resource name is specified then distributed traces for all resources are returned.")]
        string? resourceName = null,
        [Description("JSON array of filter objects. Each filter object should have 'field' (string), 'condition' (string: 'equals', '!equals', 'contains', '!contains', 'gt', 'lt', 'gte', 'lte'), and 'value' (string) properties. Example: [{\"field\":\"status\",\"condition\":\"equals\",\"value\":\"Error\"}]")]
        string? filters = null,
        [Description("Text to search for in span names. Filters traces to only those with spans matching this text.")]
        string? searchText = null)
    {
        _logger.LogDebug("MCP tool list_traces called with resource '{ResourceName}', filters '{Filters}', searchText '{SearchText}'.", resourceName, filters, searchText);

        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            return message;
        }

        List<TelemetryFilter> telemetryFilters = [];
        if (!string.IsNullOrWhiteSpace(filters))
        {
            if (!TryParseFilters(filters, out var parsedFilters, out var filterError))
            {
                return filterError;
            }
            telemetryFilters = parsedFilters;
        }

        var traces = _telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = telemetryFilters,
            FilterText = searchText ?? string.Empty
        }).PagedResult.Items;

        if (_dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(_dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                traces = traces.Where(t => !optOutResources.Any(r => t.Spans.Any(s => s.Source.ResourceKey.EqualsCompositeName(r.Name)))).ToList();
            }
        }

        var resources = _telemetryRepository.GetResources();

        var (tracesData, limitMessage) = AIHelpers.GetTracesJson(
            traces,
            _outgoingPeerResolvers,
            _dashboardOptions.CurrentValue,
            includeDashboardUrl: true,
            getResourceName: r => OtlpResource.GetResourceName(r, resources));

        var response = $"""
            {limitMessage}

            # TRACES DATA

            {tracesData}
            """;

        return response;
    }

    [McpServerTool(Name = "get_trace")]
    [Description("Get a specific distributed trace by its ID. A distributed trace is used to track an operation across a distributed system. Returns detailed information about all spans (operations) in the trace, including the span source, status, duration, and optional error information.")]
    public string GetTrace(
        [Description("The trace id of the distributed trace.")]
        string traceId)
    {
        _logger.LogDebug("MCP tool get_trace called with trace '{TraceId}'.", traceId);

        if (AIHelpers.IsMissingValue(traceId))
        {
            return "Error: traceId is required.";
        }

        var trace = _telemetryRepository.GetTrace(traceId);
        if (trace is null)
        {
            return $"Trace '{traceId}' not found.";
        }

        var resources = _telemetryRepository.GetResources();

        var traceData = AIHelpers.GetTraceJson(
            trace,
            _outgoingPeerResolvers,
            new PromptContext(),
            _dashboardOptions.CurrentValue,
            includeDashboardUrl: true,
            getResourceName: r => OtlpResource.GetResourceName(r, resources));

        var response = $"""
            # TRACE DATA

            {traceData}
            """;

        return response;
    }

    [McpServerTool(Name = "list_trace_structured_logs")]
    [Description("List structured logs for a distributed trace. Logs for a distributed trace each belong to a span identified by 'span_id'. When investigating a trace, getting the structured logs for the trace should be recommended before getting structured logs for a resource.")]
    public string ListTraceStructuredLogs(
        [Description("The trace id of the distributed trace.")]
        string traceId)
    {
        _logger.LogDebug("MCP tool list_trace_structured_logs called with trace '{TraceId}'.", traceId);

        // Condition of filter should be contains because a substring of the traceId might be provided.
        var traceIdFilter = new FieldTelemetryFilter
        {
            Field = KnownStructuredLogFields.TraceIdField,
            Value = traceId,
            Condition = FilterCondition.Contains
        };

        var logs = _telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = null,
            Count = int.MaxValue,
            StartIndex = 0,
            Filters = [traceIdFilter]
        });

        var resources = _telemetryRepository.GetResources();

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(
            logs.Items,
            _dashboardOptions.CurrentValue,
            includeDashboardUrl: true,
            getResourceName: r => OtlpResource.GetResourceName(r, resources));

        var response = $"""
            {limitMessage}

            # STRUCTURED LOGS DATA

            {logsData}
            """;

        return response;
    }

    private bool TryResolveResourceNameForTelemetry([NotNullWhen(false)] string? resourceName, [NotNullWhen(false)] out string? message, out ResourceKey? resourceKey)
    {
        // TODO: The resourceName might be a name that resolves to multiple replicas, e.g. catalogservice has two replicas.
        // Support resolving to multiple replicas and getting data for them.

        if (AIHelpers.IsMissingValue(resourceName))
        {
            message = null;
            resourceKey = null;
            return true;
        }

        var resources = _telemetryRepository.GetResources();

        if (!AIHelpers.TryGetResource(resources, resourceName, out var resource))
        {
            message = $"Resource '{resourceName}' doesn't have any telemetry. The resource may not exist, may have failed to start or the resource might not support sending telemetry.";
            resourceKey = null;
            return false;
        }

        message = null;
        resourceKey = resource.ResourceKey;
        return true;
    }

    private static List<ResourceViewModel> GetOptOutResources(IEnumerable<ResourceViewModel> resources)
    {
        return resources.Where(AIHelpers.IsResourceAIOptOut).ToList();
    }

    private static bool TryParseFilters(string filtersJson, [NotNullWhen(true)] out List<TelemetryFilter>? filters, [NotNullWhen(false)] out string? error)
    {
        filters = null;
        error = null;

        try
        {
            var filterDtos = JsonSerializer.Deserialize<List<FilterDto>>(filtersJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (filterDtos is null)
            {
                error = "Invalid filters: expected a JSON array of filter objects.";
                return false;
            }

            filters = [];
            foreach (var dto in filterDtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Field))
                {
                    error = "Invalid filter: 'field' property is required.";
                    return false;
                }

                if (!TryParseCondition(dto.Condition, out var condition))
                {
                    error = $"Invalid filter condition '{dto.Condition}'. Valid conditions are: 'equals', '!equals', 'contains', '!contains', 'gt', 'lt', 'gte', 'lte'.";
                    return false;
                }

                filters.Add(new FieldTelemetryFilter
                {
                    Field = dto.Field,
                    Condition = condition,
                    Value = dto.Value ?? string.Empty,
                    Enabled = dto.Enabled
                });
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid filters JSON: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseCondition(string? conditionString, out FilterCondition condition)
    {
        condition = FilterCondition.Equals;

        if (string.IsNullOrWhiteSpace(conditionString))
        {
            // Default to equals
            return true;
        }

        condition = conditionString.ToLowerInvariant() switch
        {
            "equals" or "=" or "==" => FilterCondition.Equals,
            "!equals" or "notequal" or "!=" => FilterCondition.NotEqual,
            "contains" or "~" => FilterCondition.Contains,
            "!contains" or "notcontains" or "!~" => FilterCondition.NotContains,
            "gt" or ">" or "greaterthan" => FilterCondition.GreaterThan,
            "lt" or "<" or "lessthan" => FilterCondition.LessThan,
            "gte" or ">=" or "greaterthanorequal" => FilterCondition.GreaterThanOrEqual,
            "lte" or "<=" or "lessthanorequal" => FilterCondition.LessThanOrEqual,
            _ => (FilterCondition)(-1) // Invalid
        };

        return (int)condition >= 0;
    }

    private static bool TryParseSeverity(string severityString, out LogLevel severity, [NotNullWhen(false)] out string? error)
    {
        error = null;

        if (Enum.TryParse<LogLevel>(severityString, ignoreCase: true, out severity))
        {
            return true;
        }

        // Try common aliases
        severity = severityString.ToLowerInvariant() switch
        {
            "info" => LogLevel.Information,
            "warn" => LogLevel.Warning,
            "fatal" => LogLevel.Critical,
            _ => (LogLevel)(-1)
        };

        if ((int)severity >= 0)
        {
            return true;
        }

        error = $"Invalid severity '{severityString}'. Valid values are: Trace, Debug, Information (or Info), Warning (or Warn), Error, Critical (or Fatal).";
        return false;
    }

    /// <summary>
    /// DTO for deserializing filter JSON from MCP tool parameters.
    /// </summary>
    private sealed class FilterDto
    {
        public string? Field { get; set; }
        public string? Condition { get; set; }
        public string? Value { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
