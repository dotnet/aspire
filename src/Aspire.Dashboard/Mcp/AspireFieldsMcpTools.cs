// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// MCP tools for discovering available telemetry fields and their values.
/// </summary>
internal sealed class AspireFieldsMcpTools
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private readonly TelemetryRepository _telemetryRepository;
    private readonly IDashboardClient _dashboardClient;
    private readonly ILogger<AspireFieldsMcpTools> _logger;

    public AspireFieldsMcpTools(
        TelemetryRepository telemetryRepository,
        IDashboardClient dashboardClient,
        ILogger<AspireFieldsMcpTools> logger)
    {
        _telemetryRepository = telemetryRepository;
        _dashboardClient = dashboardClient;
        _logger = logger;
    }

    [McpServerTool(Name = "list_telemetry_fields")]
    [Description("List available telemetry fields that can be used for filtering traces and logs. Returns known fields (built-in) and custom attribute keys discovered from telemetry data.")]
    public string ListTelemetryFields(
        [Description("The type of telemetry to list fields for. Valid values: 'traces', 'logs'. If not specified, fields for both types are returned.")]
        string? type = null,
        [Description("The resource name. If specified, only fields from the specified resource are returned.")]
        string? resourceName = null)
    {
        _logger.LogDebug("MCP tool list_telemetry_fields called with type '{Type}', resource '{ResourceName}'.", type, resourceName);

        // Validate type parameter
        var includeTraces = true;
        var includeLogs = true;
        if (!AIHelpers.IsMissingValue(type))
        {
            if (string.Equals(type, "traces", StringComparison.OrdinalIgnoreCase))
            {
                includeLogs = false;
            }
            else if (string.Equals(type, "logs", StringComparison.OrdinalIgnoreCase))
            {
                includeTraces = false;
            }
            else
            {
                return $"Invalid type '{type}'. Valid values are 'traces' or 'logs'.";
            }
        }

        // Resolve resource if specified
        ResourceKey? resourceKey = null;
        if (!AIHelpers.IsMissingValue(resourceName))
        {
            var resources = _telemetryRepository.GetResources();
            if (!AIHelpers.TryGetResource(resources, resourceName, out var resource))
            {
                return $"Resource '{resourceName}' doesn't have any telemetry. The resource may not exist, may have failed to start or the resource might not support sending telemetry.";
            }
            resourceKey = resource.ResourceKey;
        }

        var result = new Dictionary<string, object>();

        if (includeTraces)
        {
            var tracePropertyKeys = _telemetryRepository.GetTracePropertyKeys(resourceKey);
            var traceFields = new Dictionary<string, object>
            {
                ["known_fields"] = KnownTraceFields.AllFields,
                ["custom_attributes"] = tracePropertyKeys,
                ["total_count"] = KnownTraceFields.AllFields.Count + tracePropertyKeys.Count
            };
            result["traces"] = traceFields;
        }

        if (includeLogs)
        {
            var logPropertyKeys = _telemetryRepository.GetLogPropertyKeys(resourceKey);
            var logFields = new Dictionary<string, object>
            {
                ["known_fields"] = KnownStructuredLogFields.AllFields,
                ["custom_attributes"] = logPropertyKeys,
                ["total_count"] = KnownStructuredLogFields.AllFields.Count + logPropertyKeys.Count
            };
            result["logs"] = logFields;
        }

        var json = JsonSerializer.Serialize(result, s_jsonSerializerOptions);

        return $"""
            # TELEMETRY FIELDS

            These fields can be used for filtering traces and logs. Known fields are built-in fields, custom attributes are discovered from actual telemetry data.

            {json}
            """;
    }

    [McpServerTool(Name = "get_telemetry_field_values")]
    [Description("Get the distinct values for a specific telemetry field. Returns values with their occurrence counts, ordered by count descending. Use list_telemetry_fields first to discover available field names.")]
    public string GetTelemetryFieldValues(
        [Description("The field name to get values for (e.g., 'trace.status', 'http.method', 'log.level').")]
        string fieldName,
        [Description("The type of telemetry to query. Valid values: 'traces', 'logs'. If not specified, queries both types.")]
        string? type = null,
        [Description("The resource name. If specified, only values from the specified resource are returned.")]
        string? resourceName = null)
    {
        _logger.LogDebug("MCP tool get_telemetry_field_values called with fieldName '{FieldName}', type '{Type}', resource '{ResourceName}'.", fieldName, type, resourceName);

        // Validate fieldName parameter
        if (AIHelpers.IsMissingValue(fieldName))
        {
            return "The fieldName parameter is required. Use list_telemetry_fields to discover available field names.";
        }

        // Validate type parameter
        var includeTraces = true;
        var includeLogs = true;
        if (!AIHelpers.IsMissingValue(type))
        {
            if (string.Equals(type, "traces", StringComparison.OrdinalIgnoreCase))
            {
                includeLogs = false;
            }
            else if (string.Equals(type, "logs", StringComparison.OrdinalIgnoreCase))
            {
                includeTraces = false;
            }
            else
            {
                return $"Invalid type '{type}'. Valid values are 'traces' or 'logs'.";
            }
        }

        // Resolve resource if specified (currently for validation only, as GetTraceFieldValues/GetLogsFieldValues don't filter by resource)
        if (!AIHelpers.IsMissingValue(resourceName))
        {
            var resources = _telemetryRepository.GetResources();
            if (!AIHelpers.TryGetResource(resources, resourceName, out _))
            {
                return $"Resource '{resourceName}' doesn't have any telemetry. The resource may not exist, may have failed to start or the resource might not support sending telemetry.";
            }
            // Note: Currently GetTraceFieldValues and GetLogsFieldValues don't support resource filtering.
            // This validation ensures the resource exists but the returned values are from all resources.
        }

        var result = new Dictionary<string, object>();

        if (includeTraces)
        {
            var traceFieldValues = _telemetryRepository.GetTraceFieldValues(fieldName);
            var orderedTraceValues = traceFieldValues
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new Dictionary<string, object> { ["value"] = kvp.Key, ["count"] = kvp.Value })
                .ToList();
            result["traces"] = new Dictionary<string, object>
            {
                ["field"] = fieldName,
                ["values"] = orderedTraceValues,
                ["total_values"] = orderedTraceValues.Count
            };
        }

        if (includeLogs)
        {
            var logFieldValues = _telemetryRepository.GetLogsFieldValues(fieldName);
            var orderedLogValues = logFieldValues
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new Dictionary<string, object> { ["value"] = kvp.Key, ["count"] = kvp.Value })
                .ToList();
            result["logs"] = new Dictionary<string, object>
            {
                ["field"] = fieldName,
                ["values"] = orderedLogValues,
                ["total_values"] = orderedLogValues.Count
            };
        }

        var json = JsonSerializer.Serialize(result, s_jsonSerializerOptions);

        return $"""
            # TELEMETRY FIELD VALUES

            Values for field '{fieldName}', ordered by occurrence count (highest first).

            {json}
            """;
    }
}
