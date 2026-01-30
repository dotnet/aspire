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
    TelemetryRepository telemetryRepository)
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

        // TODO: Consider adding an ExcludeFromApi property on resources in the future.
        // Currently the API returns all telemetry data for all resources.

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
        else if (hasError == false)
        {
            spans = spans.Where(s => s.Status != OtlpSpanStatusCode.Error).ToList();
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

        // Filter traces by hasError
        if (hasError == true)
        {
            traces = traces.Where(t => t.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error)).ToList();
        }
        else if (hasError == false)
        {
            traces = traces.Where(t => !t.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error)).ToList();
        }

        var totalCount = traces.Count;

        // Apply limit (take from end for most recent)
        if (traces.Count > effectiveLimit)
        {
            traces = traces.Skip(traces.Count - effectiveLimit).ToList();
        }

        // Get all spans from filtered traces
        var spans = traces.SelectMany(t => t.Spans).ToList();

        var otlpData = TelemetryExportService.ConvertSpansToOtlpJson(spans);

        return new TelemetryApiResponse<OtlpTelemetryDataJson>
        {
            Data = otlpData,
            TotalCount = totalCount,
            ReturnedCount = traces.Count
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

        var spans = trace.Spans.ToList();

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
    /// Streams span updates as they arrive in OTLP JSON format.
    /// </summary>
    public async IAsyncEnumerable<string> FollowSpansAsync(
        string? resource,
        string? traceId,
        bool? hasError,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // For streaming, we don't fail on unknown resource - just filter to nothing
        var resources = telemetryRepository.GetResources();
        AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey);

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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // For streaming, we don't fail on unknown resource - just filter to nothing
        var resources = telemetryRepository.GetResources();
        AIHelpers.TryResolveResourceForTelemetry(resources, resource, out _, out var resourceKey);

        // Build filters
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

        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<LogLevel>(severity, ignoreCase: true, out var parsedLevel))
        {
            // Trace is the lowest level, so no filter needed for it
            if (parsedLevel != LogLevel.Trace)
            {
                filters.Add(new FieldTelemetryFilter
                {
                    Field = nameof(OtlpLogEntry.Severity),
                    Value = parsedLevel.ToString(),
                    Condition = FilterCondition.GreaterThanOrEqual
                });
            }
        }

        await foreach (var log in telemetryRepository.WatchLogsAsync(resourceKey, filters, cancellationToken).ConfigureAwait(false))
        {
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
