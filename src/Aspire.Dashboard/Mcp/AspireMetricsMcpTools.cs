// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Aspire.Dashboard.Mcp;

/// <summary>
/// MCP tools for querying metrics/instruments from telemetry data.
/// </summary>
internal sealed class AspireMetricsMcpTools
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private readonly TelemetryRepository _telemetryRepository;
    private readonly IDashboardClient _dashboardClient;
    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;
    private readonly ILogger<AspireMetricsMcpTools> _logger;

    public AspireMetricsMcpTools(
        TelemetryRepository telemetryRepository,
        IDashboardClient dashboardClient,
        IOptionsMonitor<DashboardOptions> dashboardOptions,
        ILogger<AspireMetricsMcpTools> logger)
    {
        _telemetryRepository = telemetryRepository;
        _dashboardClient = dashboardClient;
        _dashboardOptions = dashboardOptions;
        _logger = logger;
    }

    [McpServerTool(Name = "list_metrics")]
    [Description("List available metrics/instruments for a resource. Returns instruments grouped by meter name, including name, description, unit, and type.")]
    public string ListMetrics(
        [Description("The resource name. Required - metrics are always associated with a specific resource.")]
        string resourceName)
    {
        _logger.LogDebug("MCP tool list_metrics called with resource '{ResourceName}'.", resourceName);

        // Validate resourceName parameter - it's required for metrics
        if (AIHelpers.IsMissingValue(resourceName))
        {
            return "The resourceName parameter is required. Metrics are always associated with a specific resource. Use list_resources to discover available resources.";
        }

        // Resolve resource
        var resources = _telemetryRepository.GetResources();
        if (!AIHelpers.TryGetResource(resources, resourceName, out var resource))
        {
            return $"Resource '{resourceName}' doesn't have any telemetry. The resource may not exist, may have failed to start or the resource might not support sending telemetry.";
        }

        // Check for AI opt-out
        if (_dashboardClient.IsEnabled)
        {
            var dashboardResources = _dashboardClient.GetResources();
            var matchingResource = dashboardResources.FirstOrDefault(r => resource.ResourceKey.EqualsCompositeName(r.Name));
            if (matchingResource != null && AIHelpers.IsResourceAIOptOut(matchingResource))
            {
                return $"Resource '{resourceName}' has opted out of AI assistance.";
            }
        }

        var instruments = _telemetryRepository.GetInstrumentsSummaries(resource.ResourceKey);

        if (instruments.Count == 0)
        {
            return $"No metrics found for resource '{resourceName}'. The resource may not be emitting metrics.";
        }

        // Group instruments by meter name
        var groupedByMeter = instruments
            .GroupBy(i => i.Parent.Name)
            .OrderBy(g => g.Key)
            .Select(g => new Dictionary<string, object>
            {
                ["meter_name"] = g.Key,
                ["instruments"] = g.Select(i => new Dictionary<string, object>
                {
                    ["name"] = i.Name,
                    ["description"] = i.Description,
                    ["unit"] = i.Unit,
                    ["type"] = i.Type.ToString()
                }).ToList()
            })
            .ToList();

        var result = new Dictionary<string, object>
        {
            ["resource"] = resourceName,
            ["meters"] = groupedByMeter,
            ["total_instruments"] = instruments.Count
        };

        var json = JsonSerializer.Serialize(result, s_jsonSerializerOptions);

        return $"""
            # METRICS FOR {resourceName.ToUpperInvariant()}

            Instruments grouped by meter. Use get_metric_data to retrieve metric values for a specific instrument.

            {json}
            """;
    }

    [McpServerTool(Name = "get_metric_data")]
    [Description("Get metric data for a specific instrument. Returns dimensions with their values over the specified time window.")]
    public string GetMetricData(
        [Description("The resource name. Required - metrics are always associated with a specific resource.")]
        string resourceName,
        [Description("The meter name (e.g., 'Microsoft.AspNetCore.Hosting', 'System.Runtime').")]
        string meterName,
        [Description("The instrument name (e.g., 'http.server.request.duration', 'cpu.usage').")]
        string instrumentName,
        [Description("The time window to query. Supported values: '1m', '5m', '15m', '30m', '1h', '3h', '6h', '12h'. Default is '5m'.")]
        string? duration = null)
    {
        _logger.LogDebug("MCP tool get_metric_data called with resource '{ResourceName}', meter '{MeterName}', instrument '{InstrumentName}', duration '{Duration}'.",
            resourceName, meterName, instrumentName, duration);

        // Validate resourceName parameter
        if (AIHelpers.IsMissingValue(resourceName))
        {
            return "The resourceName parameter is required. Use list_resources to discover available resources.";
        }

        // Validate meterName parameter
        if (AIHelpers.IsMissingValue(meterName))
        {
            return "The meterName parameter is required. Use list_metrics to discover available meters and instruments.";
        }

        // Validate instrumentName parameter
        if (AIHelpers.IsMissingValue(instrumentName))
        {
            return "The instrumentName parameter is required. Use list_metrics to discover available instruments.";
        }

        // Parse duration
        if (!TryParseDuration(duration, out var timeSpan, out var durationError))
        {
            return durationError;
        }

        // Resolve resource
        var resources = _telemetryRepository.GetResources();
        if (!AIHelpers.TryGetResource(resources, resourceName, out var resource))
        {
            return $"Resource '{resourceName}' doesn't have any telemetry. The resource may not exist, may have failed to start or the resource might not support sending telemetry.";
        }

        // Check for AI opt-out
        if (_dashboardClient.IsEnabled)
        {
            var dashboardResources = _dashboardClient.GetResources();
            var matchingResource = dashboardResources.FirstOrDefault(r => resource.ResourceKey.EqualsCompositeName(r.Name));
            if (matchingResource != null && AIHelpers.IsResourceAIOptOut(matchingResource))
            {
                return $"Resource '{resourceName}' has opted out of AI assistance.";
            }
        }

        // Calculate time window
        var endTime = DateTime.UtcNow;
        var startTime = endTime - timeSpan;

        // Get instrument data
        var instrumentData = _telemetryRepository.GetInstrument(new GetInstrumentRequest
        {
            ResourceKey = resource.ResourceKey,
            MeterName = meterName,
            InstrumentName = instrumentName,
            StartTime = startTime,
            EndTime = endTime
        });

        if (instrumentData == null)
        {
            return $"Instrument '{instrumentName}' not found in meter '{meterName}' for resource '{resourceName}'. Use list_metrics to discover available instruments.";
        }

        // Build response with dimensions and values
        var dimensions = instrumentData.Dimensions.Select(d => new Dictionary<string, object>
        {
            ["name"] = d.Name,
            ["attributes"] = d.Attributes.ToDictionary(a => a.Key, a => (object)a.Value),
            ["value_count"] = d.Values.Count,
            ["latest_values"] = d.Values.TakeLast(10).Select(v => new Dictionary<string, object>
            {
                ["start"] = v.Start.ToString("O"),
                ["end"] = v.End.ToString("O"),
                ["value"] = FormatMetricValue(v)
            }).ToList()
        }).ToList();

        var result = new Dictionary<string, object>
        {
            ["resource"] = resourceName,
            ["meter"] = meterName,
            ["instrument"] = new Dictionary<string, object>
            {
                ["name"] = instrumentData.Summary.Name,
                ["description"] = instrumentData.Summary.Description,
                ["unit"] = instrumentData.Summary.Unit,
                ["type"] = instrumentData.Summary.Type.ToString()
            },
            ["time_window"] = new Dictionary<string, object>
            {
                ["start"] = startTime.ToString("O"),
                ["end"] = endTime.ToString("O"),
                ["duration"] = duration ?? "5m"
            },
            ["dimensions"] = dimensions,
            ["dimension_count"] = dimensions.Count,
            ["has_overflow"] = instrumentData.HasOverflow,
            ["known_attribute_values"] = instrumentData.KnownAttributeValues
        };

        var json = JsonSerializer.Serialize(result, s_jsonSerializerOptions);

        return $"""
            # METRIC DATA: {instrumentName}

            Metric values for the last {duration ?? "5m"}.

            {json}
            """;
    }

    private static object FormatMetricValue(Otlp.Model.MetricValues.MetricValueBase value)
    {
        return value switch
        {
            Otlp.Model.MetricValues.MetricValue<long> longValue => longValue.Value,
            Otlp.Model.MetricValues.MetricValue<double> doubleValue => doubleValue.Value,
            Otlp.Model.MetricValues.HistogramValue histogramValue => new Dictionary<string, object>
            {
                ["count"] = histogramValue.Count,
                ["sum"] = histogramValue.Sum,
                ["bucket_counts"] = histogramValue.Values,
                ["explicit_bounds"] = histogramValue.ExplicitBounds
            },
            _ => value.ToString() ?? "unknown"
        };
    }

    private static bool TryParseDuration(string? duration, out TimeSpan timeSpan, out string error)
    {
        error = string.Empty;
        timeSpan = TimeSpan.FromMinutes(5); // Default

        if (AIHelpers.IsMissingValue(duration))
        {
            return true;
        }

        var parsed = duration!.ToLowerInvariant() switch
        {
            "1m" => TimeSpan.FromMinutes(1),
            "5m" => TimeSpan.FromMinutes(5),
            "15m" => TimeSpan.FromMinutes(15),
            "30m" => TimeSpan.FromMinutes(30),
            "1h" => TimeSpan.FromHours(1),
            "3h" => TimeSpan.FromHours(3),
            "6h" => TimeSpan.FromHours(6),
            "12h" => TimeSpan.FromHours(12),
            _ => TimeSpan.MinValue
        };

        if (parsed == TimeSpan.MinValue)
        {
            error = $"Invalid duration '{duration}'. Supported values are: '1m', '5m', '15m', '30m', '1h', '3h', '6h', '12h'.";
            return false;
        }

        timeSpan = parsed;
        return true;
    }
}
