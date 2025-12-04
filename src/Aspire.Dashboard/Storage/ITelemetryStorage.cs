// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Storage;

/// <summary>
/// Interface for telemetry storage operations.
/// Provides abstraction for storing and retrieving logs, traces, and metrics.
/// </summary>
public interface ITelemetryStorage : IDisposable
{
    /// <summary>
    /// Adds log entries to storage.
    /// </summary>
    Task AddLogsAsync(IEnumerable<OtlpLogEntry> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves logs with filtering and pagination.
    /// </summary>
    Task<PagedResult<OtlpLogEntry>> GetLogsAsync(GetLogsContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific log entry by ID.
    /// </summary>
    Task<OtlpLogEntry?> GetLogAsync(long logId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds traces to storage.
    /// </summary>
    Task AddTracesAsync(IEnumerable<OtlpTrace> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves traces with filtering and pagination.
    /// </summary>
    Task<GetTracesResponse> GetTracesAsync(GetTracesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific trace by ID.
    /// </summary>
    Task<OtlpTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a resource.
    /// </summary>
    Task AddOrUpdateResourceAsync(OtlpResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources.
    /// </summary>
    Task<List<OtlpResource>> GetResourcesAsync(bool includeUninstrumentedPeers = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by key.
    /// </summary>
    Task<OtlpResource?> GetResourceAsync(ResourceKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all stored telemetry data.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of stored items by type.
    /// </summary>
    Task<(int LogCount, int TraceCount)> GetCountsAsync(CancellationToken cancellationToken = default);
}
