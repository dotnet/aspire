// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.Serialization;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Api;

/// <summary>
/// Handles telemetry API requests, returning data in OTLP JSON format.
/// </summary>
internal sealed class TelemetryApiService(
    TelemetryRepository telemetryRepository,
    IDashboardClient dashboardClient)
{
    private const int DefaultLimit = 200;
    private const int DefaultTraceLimit = 100;
    private const int MaxQueryCount = 10000;

    /// <summary>
    /// Gets spans in OTLP JSON format.
    /// Returns null if resource filter is specified but not found.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson>? GetSpans(string? resource, string? traceId, bool? hasError, int? limit)
    {
        // Validate resource exists if specified
        var resources = telemetryRepository.GetResources();
        if (!AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey))
        {
            return null;
        }

        var effectiveLimit = limit ?? DefaultLimit;

        var result = telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = MaxQueryCount,
            Filters = [],
            FilterText = string.Empty
        });

        // Extract all spans from traces
        var spans = result.PagedResult.Items.SelectMany(t => t.Spans).ToList();

        // Filter opt-out resources using HashSet for O(1) lookup
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                var optOutNames = new HashSet<string>(optOutResources.Select(r => r.Name));
                spans = spans.Where(s => !optOutNames.Contains(s.Source.ResourceKey.GetCompositeName())).ToList();
            }
        }

        // Filter by traceId
        if (!string.IsNullOrEmpty(traceId))
        {
            spans = spans.Where(s => OtlpHelpers.MatchTelemetryId(s.TraceId, traceId)).ToList();
        }

        // Filter by hasError
        if (hasError == true)
        {
            spans = spans.Where(s => s.Status == OtlpSpanStatusCode.Error).ToList();
        }

        var totalCount = spans.Count;

        // Apply limit (take from end for most recent)
        if (spans.Count > effectiveLimit)
        {
            spans = spans.Skip(spans.Count - effectiveLimit).ToList();
        }

        var otlpData = TelemetryExportService.ConvertSpansToOtlpJson(spans);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = totalCount,
            ReturnedCount = spans.Count
        };
    }

    /// <summary>
    /// Gets traces in OTLP JSON format (grouped by trace).
    /// Returns null if resource filter is specified but not found.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson>? GetTraces(string? resource, bool? hasError, int? limit)
    {
        // Validate resource exists if specified
        var resources = telemetryRepository.GetResources();
        if (!AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey))
        {
            return null;
        }

        var effectiveLimit = limit ?? DefaultTraceLimit;

        var result = telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = MaxQueryCount,
            Filters = [],
            FilterText = string.Empty
        });

        var traces = result.PagedResult.Items.ToList();

        // Build opt-out names HashSet for O(1) lookup
        HashSet<string>? optOutNames = null;
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                optOutNames = new HashSet<string>(optOutResources.Select(r => r.Name));
            }
        }

        // Filter traces
        var filteredTraces = new List<OtlpTrace>();
        foreach (var trace in traces)
        {
            // Filter opt-out resources from spans
            var hasNonOptOutSpan = optOutNames is null || 
                trace.Spans.Any(s => !optOutNames.Contains(s.Source.ResourceKey.GetCompositeName()));
            
            if (!hasNonOptOutSpan)
            {
                continue; // All spans were from opt-out resources
            }

            // Filter by hasError
            if (hasError == true && !trace.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error))
            {
                continue;
            }

            filteredTraces.Add(trace);
        }

        var totalCount = filteredTraces.Count;

        // Apply limit (take from end for most recent)
        if (filteredTraces.Count > effectiveLimit)
        {
            filteredTraces = filteredTraces.Skip(filteredTraces.Count - effectiveLimit).ToList();
        }

        // Get all spans from filtered traces, excluding opt-out resources
        var spans = filteredTraces.SelectMany(t => t.Spans).ToList();
        if (optOutNames is not null)
        {
            spans = spans.Where(s => !optOutNames.Contains(s.Source.ResourceKey.GetCompositeName())).ToList();
        }

        var otlpData = TelemetryExportService.ConvertSpansToOtlpJson(spans);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = totalCount,
            ReturnedCount = filteredTraces.Count
        };
    }

    /// <summary>
    /// Gets a specific trace by ID with all spans in OTLP format.
    /// Returns null if trace not found.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson>? GetTrace(string traceId)
    {
        var result = telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = null,
            StartIndex = 0,
            Count = MaxQueryCount,
            Filters = [],
            FilterText = string.Empty
        });

        var trace = result.PagedResult.Items.FirstOrDefault(t => OtlpHelpers.MatchTelemetryId(t.TraceId, traceId));
        if (trace is null)
        {
            return null;
        }

        // Build opt-out names HashSet for O(1) lookup
        HashSet<string>? optOutNames = null;
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                optOutNames = new HashSet<string>(optOutResources.Select(r => r.Name));
            }
        }

        // Filter spans for opt-out resources
        var spans = trace.Spans.ToList();
        if (optOutNames is not null)
        {
            spans = spans.Where(s => !optOutNames.Contains(s.Source.ResourceKey.GetCompositeName())).ToList();
        }

        if (spans.Count == 0)
        {
            return null; // All spans were from opt-out resources
        }

        var otlpData = TelemetryExportService.ConvertSpansToOtlpJson(spans);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = spans.Count,
            ReturnedCount = spans.Count
        };
    }

    /// <summary>
    /// Gets logs in OTLP JSON format.
    /// Returns null if resource filter is specified but not found.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson>? GetLogs(string? resource, string? traceId, string? severity, int? limit)
    {
        // Validate resource exists if specified
        var resources = telemetryRepository.GetResources();
        if (!AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey))
        {
            return null;
        }

        var effectiveLimit = limit ?? DefaultLimit;

        var filters = new List<TelemetryFilter>();

        if (!string.IsNullOrEmpty(traceId))
        {
            filters.Add(new FieldTelemetryFilter
            {
                Field = KnownStructuredLogFields.TraceIdField,
                Value = traceId,
                Condition = FilterCondition.Contains
            });
        }

        // Severity filter uses GreaterThanOrEqual - e.g., "error" returns Error and Critical
        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<LogLevel>(severity, ignoreCase: true, out var logLevel))
        {
            // Trace is the lowest level, so no filter needed for it
            if (logLevel != LogLevel.Trace)
            {
                filters.Add(new FieldTelemetryFilter
                {
                    Field = nameof(OtlpLogEntry.Severity),
                    Value = logLevel.ToString(),
                    Condition = FilterCondition.GreaterThanOrEqual
                });
            }
        }

        var result = telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = MaxQueryCount,
            Filters = filters
        });

        var logs = result.Items;

        // Filter opt-out resources using HashSet for O(1) lookup
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                var optOutNames = new HashSet<string>(optOutResources.Select(r => r.Name));
                logs = logs.Where(l => !optOutNames.Contains(l.ResourceView.ResourceKey.GetCompositeName())).ToList();
            }
        }

        var totalCount = logs.Count;

        // Apply limit (take from end for most recent)
        if (logs.Count > effectiveLimit)
        {
            logs = logs.Skip(logs.Count - effectiveLimit).ToList();
        }

        var otlpData = TelemetryExportService.ConvertLogsToOtlpJson(logs);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = totalCount,
            ReturnedCount = logs.Count
        };
    }

    private static List<ResourceViewModel> GetOptOutResources(IEnumerable<ResourceViewModel> resources)
    {
        return resources.Where(AIHelpers.IsResourceAIOptOut).ToList();
    }

    /// <summary>
    /// Streams span updates as they arrive in OTLP JSON format.
    /// </summary>
    public async IAsyncEnumerable<string> FollowSpansAsync(
        string? resource,
        string? traceId,
        bool? hasError,
        int? limit,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // For streaming, we don't fail on unknown resource - just filter to nothing
        var resources = telemetryRepository.GetResources();
        AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey);
        var optOutResources = dashboardClient.IsEnabled
            ? GetOptOutResources(dashboardClient.GetResources())
            : [];

        // Pre-build HashSet for O(1) lookup
        var optOutNames = optOutResources.Count > 0
            ? new HashSet<string>(optOutResources.Select(r => r.Name))
            : null;

        var count = 0;
        var isInitialBatch = true;

        await foreach (var span in telemetryRepository.WatchSpansAsync(resourceKey, cancellationToken).ConfigureAwait(false))
        {
            // Apply traceId filter
            if (!string.IsNullOrEmpty(traceId) && !OtlpHelpers.MatchTelemetryId(span.TraceId, traceId))
            {
                continue;
            }

            // Apply hasError filter
            if (hasError.HasValue && (span.Status == OtlpSpanStatusCode.Error) != hasError.Value)
            {
                continue;
            }

            // Apply opt-out resource filter
            if (optOutNames is not null && optOutNames.Contains(span.Source.ResourceKey.GetCompositeName()))
            {
                continue;
            }

            // Apply limit only to initial batch - once reached, switch to streaming mode
            if (isInitialBatch && limit.HasValue && count >= limit.Value)
            {
                isInitialBatch = false;
                // Don't yield this item - it's the first one after limit reached
                continue;
            }

            count++;
            // Use compact JSON for NDJSON streaming (no indentation)
            yield return TelemetryExportService.ConvertSpanToJson(span, logs: null, indent: false);
        }
    }

    /// <summary>
    /// Streams log updates as they arrive in OTLP JSON format.
    /// </summary>
    public async IAsyncEnumerable<string> FollowLogsAsync(
        string? resource,
        string? traceId,
        string? severity,
        int? limit,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // For streaming, we don't fail on unknown resource - just filter to nothing
        var resources = telemetryRepository.GetResources();
        AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey);
        var optOutResources = dashboardClient.IsEnabled
            ? GetOptOutResources(dashboardClient.GetResources())
            : [];

        // Pre-build HashSet for O(1) lookup
        var optOutNames = optOutResources.Count > 0
            ? new HashSet<string>(optOutResources.Select(r => r.Name))
            : null;

        // Resolve severity to LogLevel for filtering
        LogLevel? minLogLevel = null;
        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<LogLevel>(severity, ignoreCase: true, out var parsedLevel))
        {
            if (parsedLevel != LogLevel.Trace)
            {
                minLogLevel = parsedLevel;
            }
        }

        // Build filters for trace ID only (severity filtering done inline for GreaterThanOrEqual)
        var filters = new List<TelemetryFilter>();
        if (!string.IsNullOrEmpty(traceId))
        {
            filters.Add(new FieldTelemetryFilter
            {
                Field = KnownStructuredLogFields.TraceIdField,
                Value = traceId,
                Condition = FilterCondition.Contains
            });
        }

        var count = 0;
        var isInitialBatch = true;

        await foreach (var log in telemetryRepository.WatchLogsAsync(resourceKey, filters, cancellationToken).ConfigureAwait(false))
        {
            // Apply severity filter (GreaterThanOrEqual)
            if (minLogLevel.HasValue && log.Severity < minLogLevel.Value)
            {
                continue;
            }

            // Apply opt-out resource filter
            if (optOutNames is not null && optOutNames.Contains(log.ResourceView.ResourceKey.GetCompositeName()))
            {
                continue;
            }

            // Apply limit only to initial batch - once reached, switch to streaming mode
            if (isInitialBatch && limit.HasValue && count >= limit.Value)
            {
                isInitialBatch = false;
                // Don't yield this item - it's the first one after limit reached
                continue;
            }

            count++;
            var otlpData = TelemetryExportService.ConvertLogsToOtlpJson([log]);
            yield return JsonSerializer.Serialize(otlpData, OtlpJsonSerializerContext.DefaultOptions);
        }
    }
}

/// <summary>
/// Generic response wrapper for telemetry API responses.
/// </summary>
public sealed class TelemetryApiResponse<T>
{
    public required T Data { get; init; }
    public required int TotalCount { get; init; }
    public required int ReturnedCount { get; init; }
}
