// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Storage;

/// <summary>
/// In-memory implementation of ITelemetryStorage.
/// This is the default storage provider and maintains data only while the application is running.
/// </summary>
internal sealed class InMemoryTelemetryStorage : ITelemetryStorage
{
    private readonly ReaderWriterLockSlim _logsLock = new();
    private readonly ReaderWriterLockSlim _tracesLock = new();
    private readonly List<OtlpLogEntry> _logs = new();
    private readonly List<OtlpTrace> _traces = new();
    private readonly ConcurrentDictionary<ResourceKey, OtlpResource> _resources = new();
    private readonly int _maxLogCount;
    private readonly int _maxTraceCount;

    public InMemoryTelemetryStorage(int maxLogCount = 10_000, int maxTraceCount = 10_000)
    {
        _maxLogCount = maxLogCount;
        _maxTraceCount = maxTraceCount;
    }

    public Task AddLogsAsync(IEnumerable<OtlpLogEntry> logs, CancellationToken cancellationToken = default)
    {
        _logsLock.EnterWriteLock();
        try
        {
            foreach (var log in logs)
            {
                _logs.Add(log);
                
                // Remove oldest logs if we exceed the maximum
                if (_logs.Count > _maxLogCount)
                {
                    _logs.RemoveAt(0);
                }
            }
        }
        finally
        {
            _logsLock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task<PagedResult<OtlpLogEntry>> GetLogsAsync(GetLogsContext context, CancellationToken cancellationToken = default)
    {
        _logsLock.EnterReadLock();
        try
        {
            var results = _logs.AsEnumerable();

            // Apply filters from context
            foreach (var filter in context.Filters.GetEnabledFilters())
            {
                results = filter.Apply(results);
            }

            var totalCount = results.Count();
            var items = results.Skip(context.StartIndex).Take(context.Count).ToList();
            var isFull = _logs.Count >= _maxLogCount;

            return Task.FromResult(new PagedResult<OtlpLogEntry>
            {
                Items = items,
                TotalItemCount = totalCount,
                IsFull = isFull
            });
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public Task<OtlpLogEntry?> GetLogAsync(long logId, CancellationToken cancellationToken = default)
    {
        _logsLock.EnterReadLock();
        try
        {
            var log = _logs.FirstOrDefault(l => l.InternalId == logId);
            return Task.FromResult(log);
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public Task AddTracesAsync(IEnumerable<OtlpTrace> traces, CancellationToken cancellationToken = default)
    {
        _tracesLock.EnterWriteLock();
        try
        {
            foreach (var trace in traces)
            {
                _traces.Add(trace);

                // Remove oldest traces if we exceed the maximum
                if (_traces.Count > _maxTraceCount)
                {
                    _traces.RemoveAt(0);
                }
            }
        }
        finally
        {
            _tracesLock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task<GetTracesResponse> GetTracesAsync(GetTracesRequest request, CancellationToken cancellationToken = default)
    {
        _tracesLock.EnterReadLock();
        try
        {
            var results = _traces.AsEnumerable();

            // Apply filtering based on request
            if (request.ResourceKey.HasValue)
            {
                results = results.Where(t => t.Spans.Any(s => 
                    s.Source.ResourceKey == request.ResourceKey.Value));
            }

            var totalCount = results.Count();
            var items = results.Skip(request.StartIndex).Take(request.Count).ToList();
            var isFull = _traces.Count >= _maxTraceCount;

            var maxDuration = items.Count > 0 
                ? items.Max(t => t.Duration)
                : TimeSpan.Zero;

            return Task.FromResult(new GetTracesResponse
            {
                PagedResult = new PagedResult<OtlpTrace>
                {
                    Items = items,
                    TotalItemCount = totalCount,
                    IsFull = isFull
                },
                MaxDuration = maxDuration
            });
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    public Task<OtlpTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        _tracesLock.EnterReadLock();
        try
        {
            var trace = _traces.FirstOrDefault(t => t.TraceId == traceId);
            return Task.FromResult(trace);
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    public Task AddOrUpdateResourceAsync(OtlpResource resource, CancellationToken cancellationToken = default)
    {
        _resources.AddOrUpdate(resource.ResourceKey, resource, (key, existing) => resource);
        return Task.CompletedTask;
    }

    public Task<List<OtlpResource>> GetResourcesAsync(bool includeUninstrumentedPeers = false, CancellationToken cancellationToken = default)
    {
        var resources = _resources.Values.AsEnumerable();
        
        if (!includeUninstrumentedPeers)
        {
            resources = resources.Where(r => !r.UninstrumentedPeer);
        }

        return Task.FromResult(resources.OrderBy(r => r.ResourceKey).ToList());
    }

    public Task<OtlpResource?> GetResourceAsync(ResourceKey key, CancellationToken cancellationToken = default)
    {
        _resources.TryGetValue(key, out var resource);
        return Task.FromResult(resource);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _logsLock.EnterWriteLock();
        try
        {
            _logs.Clear();
        }
        finally
        {
            _logsLock.ExitWriteLock();
        }

        _tracesLock.EnterWriteLock();
        try
        {
            _traces.Clear();
        }
        finally
        {
            _tracesLock.ExitWriteLock();
        }

        _resources.Clear();

        return Task.CompletedTask;
    }

    public Task<(int LogCount, int TraceCount)> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        _logsLock.EnterReadLock();
        int logCount;
        try
        {
            logCount = _logs.Count;
        }
        finally
        {
            _logsLock.ExitReadLock();
        }

        _tracesLock.EnterReadLock();
        int traceCount;
        try
        {
            traceCount = _traces.Count;
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }

        return Task.FromResult((logCount, traceCount));
    }

    public void Dispose()
    {
        _logsLock?.Dispose();
        _tracesLock?.Dispose();
    }
}
