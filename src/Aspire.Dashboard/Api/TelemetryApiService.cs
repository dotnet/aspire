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

    /// <summary>
    /// Gets traces in OTLP JSON format.
    /// Returns null if resource filter is specified but not found.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson>? GetTraces(string? resource, bool? hasError, int? limit)
    {
        // Validate resource exists if specified
        if (!TryResolveResourceKey(resource, out var resourceKey))
        {
            return null;
        }

        var effectiveLimit = limit ?? DefaultLimit;

        var result = telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = resourceKey,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = [],
            FilterText = string.Empty
        });

        var traces = result.PagedResult.Items;

        // Filter opt-out resources
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                traces = traces.Where(t => !optOutResources.Any(r =>
                    t.Spans.Any(s => s.Source.ResourceKey.EqualsCompositeName(r.Name)))).ToList();
            }
        }

        // Filter by hasError
        if (hasError == true)
        {
            traces = traces.Where(t => t.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error)).ToList();
        }

        var totalCount = traces.Count;

        // Apply limit (take from end for most recent)
        if (traces.Count > effectiveLimit)
        {
            traces = traces.Skip(traces.Count - effectiveLimit).ToList();
        }

        var otlpData = TelemetryExportService.ConvertTracesToOtlpJson(traces);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = totalCount,
            ReturnedCount = traces.Count
        };
    }

    /// <summary>
    /// Gets a single trace by ID in OTLP JSON format.
    /// </summary>
    public string? GetTraceById(string traceId)
    {
        var trace = telemetryRepository.GetTrace(traceId);

        if (trace is null)
        {
            return null;
        }

        return TelemetryExportService.ConvertTraceToJson(trace);
    }

    /// <summary>
    /// Gets logs for a trace in OTLP JSON format.
    /// Returns empty results if no logs match the trace ID.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson> GetTraceLogs(string traceId)
    {
        // Use Contains filter because a substring of the traceId might be provided
        var traceIdFilter = new FieldTelemetryFilter
        {
            Field = KnownStructuredLogFields.TraceIdField,
            Value = traceId,
            Condition = FilterCondition.Contains
        };

        var result = telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = null,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = [traceIdFilter]
        });

        var logs = result.Items;
        var otlpData = TelemetryExportService.ConvertLogsToOtlpJson(logs);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = logs.Count,
            ReturnedCount = logs.Count
        };
    }

    /// <summary>
    /// Gets logs in OTLP JSON format.
    /// Returns null if resource filter is specified but not found.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson>? GetLogs(string? resource, string? traceId, string? severity, int? limit)
    {
        // Validate resource exists if specified
        if (!TryResolveResourceKey(resource, out var resourceKey))
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
            Count = int.MaxValue,
            Filters = filters
        });

        var logs = result.Items;

        // Filter opt-out resources
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                logs = logs.Where(l => !optOutResources.Any(r =>
                    l.ResourceView.ResourceKey.EqualsCompositeName(r.Name))).ToList();
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

    /// <summary>
    /// Tries to resolve the resource name for telemetry.
    /// Returns true if no resource was specified or if the resource was found.
    /// </summary>
    private bool TryResolveResourceKey(string? resourceName, out ResourceKey? resourceKey)
    {
        if (AIHelpers.IsMissingValue(resourceName))
        {
            resourceKey = null;
            return true;
        }

        var resources = telemetryRepository.GetResources();
        if (!AIHelpers.TryGetResource(resources, resourceName, out var resource))
        {
            resourceKey = null;
            return false;
        }

        resourceKey = resource.ResourceKey;
        return true;
    }

    private static List<ResourceViewModel> GetOptOutResources(IEnumerable<ResourceViewModel> resources)
    {
        return resources.Where(AIHelpers.IsResourceAIOptOut).ToList();
    }

    /// <summary>
    /// Streams trace updates as they arrive in OTLP JSON format.
    /// </summary>
    public async IAsyncEnumerable<string> FollowTracesAsync(
        string? resource,
        bool? hasError,
        int? limit,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // For streaming, we don't fail on unknown resource - just filter to nothing
        TryResolveResourceKey(resource, out var resourceKey);
        var optOutResources = dashboardClient.IsEnabled
            ? GetOptOutResources(dashboardClient.GetResources())
            : [];

        var count = 0;
        var isInitialBatch = true;

        await foreach (var trace in telemetryRepository.WatchTracesAsync(resourceKey, cancellationToken).ConfigureAwait(false))
        {
            // Apply hasError filter
            var traceHasError = trace.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error);
            if (hasError.HasValue && traceHasError != hasError.Value)
            {
                continue;
            }

            // Apply opt-out resource filter
            if (optOutResources.Count > 0 && optOutResources.Any(r =>
                trace.Spans.Any(s => s.Source.ResourceKey.EqualsCompositeName(r.Name))))
            {
                continue;
            }

            // Apply limit only to initial batch - once we hit the limit,
            // switch to streaming mode (no limit) for new items
            if (isInitialBatch && limit.HasValue)
            {
                if (count >= limit.Value)
                {
                    // Limit reached - stop limiting, but still yield this trace
                    // as it's the first "new" trace after the initial batch
                    isInitialBatch = false;
                }
            }

            count++;
            yield return TelemetryExportService.ConvertTraceToJson(trace);
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
        TryResolveResourceKey(resource, out var resourceKey);
        var optOutResources = dashboardClient.IsEnabled
            ? GetOptOutResources(dashboardClient.GetResources())
            : [];

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
            if (optOutResources.Count > 0 && optOutResources.Any(r =>
                log.ResourceView.ResourceKey.EqualsCompositeName(r.Name)))
            {
                continue;
            }

            // Apply limit only to initial batch - once we hit the limit,
            // switch to streaming mode (no limit) for new items
            if (isInitialBatch && limit.HasValue)
            {
                if (count >= limit.Value)
                {
                    // Limit reached - stop limiting, but still yield this log
                    // as it's the first "new" log after the initial batch
                    isInitialBatch = false;
                }
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
