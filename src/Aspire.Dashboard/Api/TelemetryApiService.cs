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
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson> GetTraces(string? resource, bool? hasError, int? limit)
    {
        var resourceKey = ResolveResourceKey(resource);
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
        // Get all traces since we need to search by shortened or full ID
        var result = telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = null,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = [],
            FilterText = string.Empty
        });

        // Match against both shortened ID (prefix match) and full ID (exact match)
        var trace = result.PagedResult.Items.FirstOrDefault(t =>
            t.TraceId.StartsWith(traceId, StringComparison.OrdinalIgnoreCase) ||
            OtlpHelpers.ToShortenedId(t.TraceId).Equals(traceId, StringComparison.OrdinalIgnoreCase));

        if (trace is null)
        {
            return null;
        }

        return TelemetryExportService.ConvertTraceToJson(trace);
    }

    /// <summary>
    /// Gets logs for a trace in OTLP JSON format.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson> GetTraceLogs(string traceId)
    {
        // First, resolve the shortened trace ID to a full trace ID if needed
        var fullTraceId = ResolveFullTraceId(traceId);
        if (fullTraceId is null)
        {
            return new TelemetryApiResponse<OtlpTelemetryDataJson>
            {
                Data = new OtlpTelemetryDataJson(),
                TotalCount = 0,
                ReturnedCount = 0
            };
        }

        var logs = telemetryRepository.GetLogsForTrace(fullTraceId);
        var otlpData = TelemetryExportService.ConvertLogsToOtlpJson(logs);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = logs.Count,
            ReturnedCount = logs.Count
        };
    }

    private string? ResolveFullTraceId(string traceId)
    {
        // If the trace ID looks like a full ID (32 hex chars for trace ID), use it directly
        if (traceId.Length >= 32)
        {
            return traceId;
        }

        // Otherwise, search for a trace that matches the shortened ID
        var result = telemetryRepository.GetTraces(new GetTracesRequest
        {
            ResourceKey = null,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = [],
            FilterText = string.Empty
        });

        var trace = result.PagedResult.Items.FirstOrDefault(t =>
            t.TraceId.StartsWith(traceId, StringComparison.OrdinalIgnoreCase) ||
            OtlpHelpers.ToShortenedId(t.TraceId).Equals(traceId, StringComparison.OrdinalIgnoreCase));

        return trace?.TraceId;
    }

    /// <summary>
    /// Gets logs in OTLP JSON format.
    /// </summary>
    public TelemetryApiResponse<OtlpTelemetryDataJson> GetLogs(string? resource, string? traceId, string? severity, int? limit)
    {
        var resourceKey = ResolveResourceKey(resource);
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

        if (!string.IsNullOrEmpty(severity))
        {
            filters.Add(new FieldTelemetryFilter
            {
                Field = KnownStructuredLogFields.LevelField,
                Value = severity,
                Condition = FilterCondition.Equals
            });
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
    /// Gets a single log entry by ID in OTLP JSON format.
    /// </summary>
    public string? GetLogById(long logId)
    {
        var result = telemetryRepository.GetLogs(new GetLogsContext
        {
            ResourceKey = null,
            StartIndex = 0,
            Count = int.MaxValue,
            Filters = []
        });

        var log = result.Items.FirstOrDefault(l => l.InternalId == logId);

        if (log is null)
        {
            return null;
        }

        var otlpData = TelemetryExportService.ConvertLogsToOtlpJson([log]);
        return JsonSerializer.Serialize(otlpData, OtlpJsonSerializerContext.IndentedOptions);
    }

    private ResourceKey? ResolveResourceKey(string? resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }

        var resources = telemetryRepository.GetResources();
        var resource = resources.FirstOrDefault(r =>
            r.ResourceName.Equals(resourceName, StringComparison.OrdinalIgnoreCase) ||
            r.ResourceKey.ToString().Equals(resourceName, StringComparison.OrdinalIgnoreCase));

        return resource?.ResourceKey;
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
        var resourceKey = ResolveResourceKey(resource);
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

            // Apply limit only to initial batch
            if (isInitialBatch && limit.HasValue && count >= limit.Value)
            {
                isInitialBatch = false;
                continue;
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
        var resourceKey = ResolveResourceKey(resource);
        var optOutResources = dashboardClient.IsEnabled
            ? GetOptOutResources(dashboardClient.GetResources())
            : [];

        // Resolve traceId once before the loop
        var fullTraceId = !string.IsNullOrEmpty(traceId) ? ResolveFullTraceId(traceId) : null;

        // Build filters
        var filters = new List<TelemetryFilter>();
        if (fullTraceId is not null)
        {
            filters.Add(new FieldTelemetryFilter
            {
                Field = KnownStructuredLogFields.TraceIdField,
                Value = fullTraceId,
                Condition = FilterCondition.Contains
            });
        }
        if (!string.IsNullOrEmpty(severity))
        {
            filters.Add(new FieldTelemetryFilter
            {
                Field = KnownStructuredLogFields.LevelField,
                Value = severity,
                Condition = FilterCondition.Equals
            });
        }

        var count = 0;
        var isInitialBatch = true;

        await foreach (var log in telemetryRepository.WatchLogsAsync(resourceKey, filters, cancellationToken).ConfigureAwait(false))
        {
            // Apply opt-out resource filter
            if (optOutResources.Count > 0 && optOutResources.Any(r =>
                log.ResourceView.ResourceKey.EqualsCompositeName(r.Name)))
            {
                continue;
            }

            // Apply limit only to initial batch
            if (isInitialBatch && limit.HasValue && count >= limit.Value)
            {
                isInitialBatch = false;
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
