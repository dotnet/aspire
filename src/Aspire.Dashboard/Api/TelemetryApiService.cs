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
            Count = int.MaxValue,
            Filters = [],
            FilterText = string.Empty
        });

        // Extract all spans from traces
        var spans = result.PagedResult.Items.SelectMany(t => t.Spans).ToList();

        // Filter opt-out resources
        if (dashboardClient.IsEnabled)
        {
            var optOutResources = GetOptOutResources(dashboardClient.GetResources());
            if (optOutResources.Count > 0)
            {
                spans = spans.Where(s => !optOutResources.Any(r =>
                    s.Source.ResourceKey.EqualsCompositeName(r.Name))).ToList();
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
    /// Gets a single span by ID in OTLP JSON format.
    /// </summary>
    public string? GetSpanById(string spanId)
    {
        var span = telemetryRepository.GetSpan(spanId);

        if (span is null)
        {
            return null;
        }

        return TelemetryExportService.ConvertSpanToJson(span);
    }

    /// <summary>
    /// Gets logs for a trace in OTLP JSON format.
    /// Returns empty results if no logs match the trace ID.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson> GetSpanLogs(string traceId)
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
            if (optOutResources.Count > 0 && optOutResources.Any(r =>
                span.Source.ResourceKey.EqualsCompositeName(r.Name)))
            {
                continue;
            }

            // Apply limit only to initial batch
            if (isInitialBatch && limit.HasValue)
            {
                if (count >= limit.Value)
                {
                    isInitialBatch = false;
                }
            }

            count++;
            yield return TelemetryExportService.ConvertSpanToJson(span);
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
