// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Api;
using Aspire.Dashboard.Model.Assistant;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Api;

/// <summary>
/// Handles telemetry API requests, converting internal models to API DTOs.
/// </summary>
internal sealed class TelemetryApiService(
    TelemetryRepository telemetryRepository,
    IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers,
    IOptionsMonitor<DashboardOptions> dashboardOptions,
    IDashboardClient dashboardClient)
{
    private const int DefaultLimit = 200;

    public TracesResponse GetTraces(string? resource, bool? hasError, int? limit)
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

        var resources = telemetryRepository.GetResources();
        var traceDtos = traces.Select(t => MapToTraceDto(t, resources)).ToArray();

        return new TracesResponse
        {
            Traces = traceDtos,
            TotalCount = totalCount,
            ReturnedCount = traceDtos.Length
        };
    }

    public TraceDto? GetTraceById(string traceId)
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

        var resources = telemetryRepository.GetResources();
        return MapToTraceDto(trace, resources);
    }

    public LogsResponse GetTraceLogs(string traceId)
    {
        // First, resolve the shortened trace ID to a full trace ID if needed
        var fullTraceId = ResolveFullTraceId(traceId);
        if (fullTraceId is null)
        {
            // No matching trace found, return empty
            return new LogsResponse
            {
                Logs = [],
                TotalCount = 0,
                ReturnedCount = 0
            };
        }

        var logs = telemetryRepository.GetLogsForTrace(fullTraceId);
        var resources = telemetryRepository.GetResources();
        var logDtos = logs.Select(l => MapToLogEntryDto(l, resources)).ToArray();

        return new LogsResponse
        {
            Logs = logDtos,
            TotalCount = logDtos.Length,
            ReturnedCount = logDtos.Length
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

    public LogsResponse GetLogs(string? resource, string? traceId, string? severity, int? limit)
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

        var resources = telemetryRepository.GetResources();
        var logDtos = logs.Select(l => MapToLogEntryDto(l, resources)).ToArray();

        return new LogsResponse
        {
            Logs = logDtos,
            TotalCount = totalCount,
            ReturnedCount = logDtos.Length
        };
    }

    public LogEntryDto? GetLogById(long logId)
    {
        // Search through logs to find by internal ID
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

        var resources = telemetryRepository.GetResources();
        return MapToLogEntryDto(log, resources);
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

    private TraceDto MapToTraceDto(OtlpTrace trace, List<OtlpResource> resources)
    {
        var options = dashboardOptions.CurrentValue;

        var spans = trace.Spans.Select(s => new SpanDto
        {
            SpanId = OtlpHelpers.ToShortenedId(s.SpanId),
            ParentSpanId = s.ParentSpanId is { } id ? OtlpHelpers.ToShortenedId(id) : null,
            Kind = s.Kind.ToString(),
            Name = s.Name,
            Status = s.Status != OtlpSpanStatusCode.Unset ? s.Status.ToString() : null,
            StatusMessage = s.StatusMessage,
            Source = OtlpResource.GetResourceName(s.Source, resources),
            Destination = ResolveDestination(s),
            DurationMs = (int)Math.Round(s.Duration.TotalMilliseconds, 0, MidpointRounding.AwayFromZero),
            Attributes = s.Attributes.ToDictionary(a => a.Key, a => a.Value)
        }).ToArray();

        var traceId = OtlpHelpers.ToShortenedId(trace.TraceId);

        return new TraceDto
        {
            TraceId = traceId,
            DurationMs = (int)Math.Round(trace.Duration.TotalMilliseconds, 0, MidpointRounding.AwayFromZero),
            Title = trace.RootOrFirstSpan.Name,
            HasError = trace.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error),
            Timestamp = trace.TimeStamp,
            Spans = spans,
            DashboardLink = GetDashboardLink(options, DashboardUrls.TraceDetailUrl(traceId), traceId)
        };
    }

    private LogEntryDto MapToLogEntryDto(OtlpLogEntry log, List<OtlpResource> resources)
    {
        var options = dashboardOptions.CurrentValue;
        var exceptionText = OtlpLogEntry.GetExceptionText(log);

        return new LogEntryDto
        {
            LogId = log.InternalId,
            TraceId = log.TraceId is { } tid ? OtlpHelpers.ToShortenedId(tid) : null,
            SpanId = log.SpanId is { } sid ? OtlpHelpers.ToShortenedId(sid) : null,
            Message = log.Message,
            Severity = log.Severity.ToString(),
            ResourceName = OtlpResource.GetResourceName(log.ResourceView, resources),
            Timestamp = log.TimeStamp,
            Attributes = log.Attributes
                .Where(a => a.Key is not (OtlpLogEntry.ExceptionStackTraceField or OtlpLogEntry.ExceptionMessageField or OtlpLogEntry.ExceptionTypeField))
                .ToDictionary(a => a.Key, a => a.Value),
            Exception = exceptionText,
            Source = log.Scope.Name,
            DashboardLink = GetDashboardLink(options, DashboardUrls.StructuredLogsUrl(logEntryId: log.InternalId), $"logId: {log.InternalId}")
        };
    }

    private string? ResolveDestination(OtlpSpan span)
    {
        foreach (var resolver in outgoingPeerResolvers)
        {
            if (resolver.TryResolvePeer(span.Attributes, out var name, out _))
            {
                return name;
            }
        }

        return span.Attributes.GetPeerAddress();
    }

    private static LinkDto? GetDashboardLink(DashboardOptions options, string path, string text)
    {
        var frontendEndpoints = options.Frontend.GetEndpointAddresses();

        var frontendUrl = options.Frontend.PublicUrl
            ?? frontendEndpoints.FirstOrDefault(e => string.Equals(e.Scheme, "https", StringComparison.Ordinal))?.ToString()
            ?? frontendEndpoints.FirstOrDefault(e => string.Equals(e.Scheme, "http", StringComparison.Ordinal))?.ToString();

        if (frontendUrl is null)
        {
            return null;
        }

        return new LinkDto
        {
            Url = new Uri(new Uri(frontendUrl), path).ToString(),
            Text = text
        };
    }

    private static List<ResourceViewModel> GetOptOutResources(IEnumerable<ResourceViewModel> resources)
    {
        return resources.Where(AIHelpers.IsResourceAIOptOut).ToList();
    }

    /// <summary>
    /// Streams trace updates as they arrive.
    /// </summary>
    /// <param name="resource">Filter by resource name.</param>
    /// <param name="hasError">Filter by error status.</param>
    /// <param name="limit">Maximum number of initial traces to send (null = all existing traces).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async IAsyncEnumerable<TraceDto> FollowTracesAsync(
        string? resource,
        bool? hasError,
        int? limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var resourceKey = ResolveResourceKey(resource);
        var channel = System.Threading.Channels.Channel.CreateUnbounded<bool>();

        // Track which traces we've already sent
        var sentTraceIds = new HashSet<string>();
        var isInitialBatch = true;

        // Subscribe to new traces
        using var subscription = telemetryRepository.OnNewTraces(resourceKey, SubscriptionType.Read, () =>
        {
            channel.Writer.TryWrite(true);
            return Task.CompletedTask;
        });

        // Initial send of existing traces
        channel.Writer.TryWrite(true);

        await foreach (var _ in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var result = telemetryRepository.GetTraces(new GetTracesRequest
            {
                ResourceKey = resourceKey,
                StartIndex = 0,
                Count = int.MaxValue,
                Filters = [],
                FilterText = string.Empty
            });

            var resources = telemetryRepository.GetResources();

            // For initial batch with limit, take only the last N traces
            var traces = result.PagedResult.Items.AsEnumerable();
            if (isInitialBatch && limit.HasValue && limit.Value > 0)
            {
                traces = traces.TakeLast(limit.Value);
            }

            foreach (var trace in traces)
            {
                // Skip if we've already sent this trace
                if (!sentTraceIds.Add(trace.TraceId))
                {
                    continue;
                }

                // Apply hasError filter
                var traceHasError = trace.Spans.Any(s => s.Status == OtlpSpanStatusCode.Error);
                if (hasError.HasValue && traceHasError != hasError.Value)
                {
                    continue;
                }

                yield return MapToTraceDto(trace, resources);
            }

            isInitialBatch = false;
        }
    }

    /// <summary>
    /// Streams log updates as they arrive.
    /// </summary>
    /// <param name="resource">Filter by resource name.</param>
    /// <param name="traceId">Filter by trace ID.</param>
    /// <param name="severity">Filter by log severity.</param>
    /// <param name="limit">Maximum number of initial logs to send (null = all existing logs).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async IAsyncEnumerable<LogEntryDto> FollowLogsAsync(
        string? resource,
        string? traceId,
        string? severity,
        int? limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var resourceKey = ResolveResourceKey(resource);
        var channel = System.Threading.Channels.Channel.CreateUnbounded<bool>();

        // Track the last log ID we've sent
        long lastSentLogId = 0;
        var isInitialBatch = true;

        // Subscribe to new logs
        using var subscription = telemetryRepository.OnNewLogs(resourceKey, SubscriptionType.Read, () =>
        {
            channel.Writer.TryWrite(true);
            return Task.CompletedTask;
        });

        // Initial send
        channel.Writer.TryWrite(true);

        await foreach (var _ in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var filters = new List<TelemetryFilter>();

            if (!string.IsNullOrEmpty(traceId))
            {
                var fullTraceId = ResolveFullTraceId(traceId);
                if (fullTraceId is not null)
                {
                    filters.Add(new FieldTelemetryFilter
                    {
                        Field = KnownStructuredLogFields.TraceIdField,
                        Value = fullTraceId,
                        Condition = FilterCondition.Contains
                    });
                }
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

            var resources = telemetryRepository.GetResources();

            // Get new logs since last sent
            var newLogs = result.Items.Where(l => l.InternalId > lastSentLogId);

            // For initial batch with limit, take only the last N logs
            if (isInitialBatch && limit.HasValue && limit.Value > 0)
            {
                newLogs = newLogs.TakeLast(limit.Value);
            }

            foreach (var log in newLogs)
            {
                lastSentLogId = log.InternalId;
                yield return MapToLogEntryDto(log, resources);
            }

            isInitialBatch = false;
        }
    }
}
