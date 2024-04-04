// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Watches for updates to the collection of resolved endpoints for a specified service.
/// </summary>
internal sealed partial class ServiceEndPointWatcher(
    IServiceEndPointProvider[] resolvers,
    ILogger logger,
    string serviceName,
    TimeProvider timeProvider,
    IOptions<ServiceDiscoveryOptions> options) : IAsyncDisposable
{
    private static readonly TimerCallback s_pollingAction = static state => _ = ((ServiceEndPointWatcher)state!).RefreshAsync(force: true);

    private readonly object _lock = new();
    private readonly ILogger _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ServiceDiscoveryOptions _options = options.Value;
    private readonly IServiceEndPointProvider[] _resolvers = resolvers;
    private readonly CancellationTokenSource _disposalCancellation = new();
    private ITimer? _pollingTimer;
    private ServiceEndPointSource? _cachedEndPoints;
    private Task _refreshTask = Task.CompletedTask;
    private volatile CacheStatus _cacheState;

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; } = serviceName;

    /// <summary>
    /// Gets or sets the action called when endpoints are updated.
    /// </summary>
    public Action<ServiceEndPointResolverResult>? OnEndPointsUpdated { get; set; }

    /// <summary>
    /// Starts the endpoint resolver.
    /// </summary>
    public void Start()
    {
        ThrowIfNoResolvers();
        _ = RefreshAsync(force: false);
    }

    /// <summary>
    /// Returns a collection of resolved endpoints for the service.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A collection of resolved endpoints for the service.</returns>
    public ValueTask<ServiceEndPointSource> GetEndPointsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNoResolvers();

        // If the cache is valid, return the cached value.
        if (_cachedEndPoints is { ChangeToken.HasChanged: false } cached)
        {
            return new ValueTask<ServiceEndPointSource>(cached);
        }

        // Otherwise, ensure the cache is being refreshed
        // Wait for the cache refresh to complete and return the cached value.
        return GetEndPointsInternal(cancellationToken);

        async ValueTask<ServiceEndPointSource> GetEndPointsInternal(CancellationToken cancellationToken)
        {
            ServiceEndPointSource? result;
            do
            {
                await RefreshAsync(force: false).WaitAsync(cancellationToken).ConfigureAwait(false);
                result = _cachedEndPoints;
            } while (result is null);
            return result;
        }
    }

    // Ensures that there is a refresh operation running, if needed, and returns the task which represents the completion of the operation
    private Task RefreshAsync(bool force)
    {
        lock (_lock)
        {
            // If the cache is invalid or needs invalidation, refresh the cache.
            if (_refreshTask.IsCompleted && (_cacheState == CacheStatus.Invalid || _cachedEndPoints is null or { ChangeToken.HasChanged: true } || force))
            {
                // Indicate that the cache is being updated and start a new refresh task.
                _cacheState = CacheStatus.Refreshing;

                // Don't capture the current ExecutionContext and its AsyncLocals onto the callback.
                var restoreFlow = false;
                try
                {
                    if (!ExecutionContext.IsFlowSuppressed())
                    {
                        ExecutionContext.SuppressFlow();
                        restoreFlow = true;
                    }

                    _refreshTask = RefreshAsyncInternal();
                }
                finally
                {
                    if (restoreFlow)
                    {
                        ExecutionContext.RestoreFlow();
                    }
                }
            }

            return _refreshTask;
        }
    }

    private async Task RefreshAsyncInternal()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        var cancellationToken = _disposalCancellation.Token;
        Exception? error = null;
        ServiceEndPointSource? newEndPoints = null;
        CacheStatus newCacheState;
        try
        {
            Log.ResolvingEndPoints(_logger, ServiceName);
            var builder = new ServiceEndPointBuilder();
            foreach (var resolver in _resolvers)
            {
                await resolver.PopulateAsync(builder, cancellationToken).ConfigureAwait(false);
            }

            var endPoints = builder.Build();
            newCacheState = CacheStatus.Valid;

            lock (_lock)
            {
                // Check if we need to poll for updates or if we can register for change notification callbacks.
                if (endPoints.ChangeToken.ActiveChangeCallbacks)
                {
                    // Initiate a background refresh, if necessary.
                    endPoints.ChangeToken.RegisterChangeCallback(static state => _ = ((ServiceEndPointWatcher)state!).RefreshAsync(force: false), this);
                    if (_pollingTimer is { } timer)
                    {
                        _pollingTimer = null;
                        timer.Dispose();
                    }
                }
                else
                {
                    SchedulePollingTimer();
                }

                // The cache is valid
                newEndPoints = endPoints;
                newCacheState = CacheStatus.Valid;
            }
        }
        catch (Exception exception)
        {
            error = exception;
            newCacheState = CacheStatus.Invalid;
            SchedulePollingTimer();
        }

        // If there was an error, the cache must be invalid.
        Debug.Assert(error is null || newCacheState is CacheStatus.Invalid);

        // To ensure coherence between the value returned by calls made to GetEndPointsAsync and value passed to the callback,
        // we invalidate the cache before invoking the callback. This causes callers to wait on the refresh task
        // before receiving the updated value. An alternative approach is to lock access to _cachedEndPoints, but
        // that will have more overhead in the common case.
        if (newCacheState is CacheStatus.Valid)
        {
            Interlocked.Exchange(ref _cachedEndPoints, null);
        }

        if (OnEndPointsUpdated is { } callback)
        {
            callback(new(newEndPoints, error));
        }

        lock (_lock)
        {
            if (newCacheState is CacheStatus.Valid)
            {
                Debug.Assert(newEndPoints is not null);
                _cachedEndPoints = newEndPoints;
            }

            _cacheState = newCacheState;
        }

        if (error is not null)
        {
            Log.ResolutionFailed(_logger, error, ServiceName);
            ExceptionDispatchInfo.Throw(error);
        }
        else if (newEndPoints is not null)
        {
            Log.ResolutionSucceeded(_logger, ServiceName, newEndPoints);
        }
    }

    private void SchedulePollingTimer()
    {
        lock (_lock)
        {
            if (_pollingTimer is null)
            {
                _pollingTimer = _timeProvider.CreateTimer(s_pollingAction, this, _options.RefreshPeriod, TimeSpan.Zero);
            }
            else
            {
                _pollingTimer.Change(_options.RefreshPeriod, TimeSpan.Zero);
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            if (_pollingTimer is { } timer)
            {
                _pollingTimer = null;
                timer.Dispose();
            }
        }

        _disposalCancellation.Cancel();
        if (_refreshTask is { } task)
        {
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        foreach (var resolver in _resolvers)
        {
            await resolver.DisposeAsync().ConfigureAwait(false);
        }
    }

    private enum CacheStatus
    {
        Invalid,
        Refreshing,
        Valid
    }

    private void ThrowIfNoResolvers()
    {
        if (_resolvers.Length == 0)
        {
            ThrowNoResolversConfigured();
        }
    }

    [DoesNotReturn]
    private static void ThrowNoResolversConfigured() => throw new InvalidOperationException("No service endpoint resolvers are configured.");
}
