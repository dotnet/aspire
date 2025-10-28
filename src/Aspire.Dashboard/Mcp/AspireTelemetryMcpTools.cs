// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// MCP tools for telemetry data that don't require a resource service.
/// </summary>
internal sealed class AspireTelemetryMcpTools
{
    private readonly TelemetryRepository _telemetryRepository;
    private readonly IEnumerable<IOutgoingPeerResolver> _outgoingPeerResolvers;

    public AspireTelemetryMcpTools(TelemetryRepository telemetryRepository, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        _telemetryRepository = telemetryRepository;
        _outgoingPeerResolvers = outgoingPeerResolvers;
    }

    [McpServerTool(Name = "list_structured_logs")]
    [Description("List structured logs for resources.")]
    public string ListStructuredLogs(
        [Description("The resource name. This limits logs returned to the specified resource. If no resource name is specified then structured logs for all resources are returned.")]
        string? resourceName = null)
    {
        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            return message;
        }

        // Get all logs because we want the most recent logs and they're at the end of the results.
        // If support is added for ordering logs by timestamp then improve this.
        var logs = _telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = []
        });

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(logs.Items);

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
        string? resourceName = null)
    {
        if (!TryResolveResourceNameForTelemetry(resourceName, out var message, out var resourceKey))
        {
            return message;
        }

        var traces = _telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = [],
            FilterText = string.Empty
        });

        var (tracesData, limitMessage) = AIHelpers.GetTracesJson(traces.PagedResult.Items, _outgoingPeerResolvers);

        var response = $"""
            {limitMessage}

            # TRACES DATA

            {tracesData}
            """;

        return response;
    }

    [McpServerTool(Name = "list_trace_structured_logs")]
    [Description("List structured logs for a distributed trace. Logs for a distributed trace each belong to a span identified by 'span_id'. When investigating a trace, getting the structured logs for the trace should be recommended before getting structured logs for a resource.")]
    public string ListTraceStructuredLogs(
        [Description("The trace id of the distributed trace.")]
        string traceId)
    {
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

        var (logsData, limitMessage) = AIHelpers.GetStructuredLogsJson(logs.Items);

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
}
