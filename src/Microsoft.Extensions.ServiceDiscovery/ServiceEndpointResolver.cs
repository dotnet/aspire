// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Resolves service names to collections of endpoints.
/// </summary>
public sealed class ServiceEndpointResolver : IAsyncDisposable
{
    private static readonly TimerCallback s_cleanupCallback = s => ((ServiceEndpointResolver)s!).CleanupResolvers();
    private static readonly TimeSpan s_cleanupPeriod = TimeSpan.FromSeconds(10);

    private readonly object _lock = new();
    private readonly ServiceEndpointWatcherFactory _watcherFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, ResolverEntry> _resolvers = new();
    private ITimer? _cleanupTimer;
    private Task? _cleanupTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceEndpointResolver"/> class.
    /// </summary>
    /// <param name="watcherFactory">The watcher factory.</param>
    /// <param name="timeProvider">The time provider.</param>
    internal ServiceEndpointResolver(ServiceEndpointWatcherFactory watcherFactory, TimeProvider timeProvider)
    {
        _watcherFactory = watcherFactory;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Resolves and returns service endpoints for the specified service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The resolved service endpoints.</returns>
    public async ValueTask<ServiceEndpointSource> GetEndpointsAsync(string serviceName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureCleanupTimerStarted();

        while (true)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            var resolver = _resolvers.GetOrAdd(
                serviceName,
                static (name, self) => self.CreateResolver(name),
                this);

            var (valid, result) = await resolver.GetEndpointsAsync(cancellationToken).ConfigureAwait(false);
            if (valid)
            {
                if (result is null)
                {
                    throw new InvalidOperationException($"Unable to resolve endpoints for service {resolver.ServiceName}");
                }

                return result;
            }
            else
            {
                _resolvers.TryRemove(KeyValuePair.Create(resolver.ServiceName, resolver));
            }
        }
    }

    private void EnsureCleanupTimerStarted()
    {
        if (_cleanupTimer is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_cleanupTimer is not null)
            {
                return;
            }

            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer
            var restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                _cleanupTimer = _timeProvider.CreateTimer(s_cleanupCallback, this, s_cleanupPeriod, s_cleanupPeriod);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            _disposed = true;
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;
        }

        foreach (var resolver in _resolvers)
        {
            await resolver.Value.DisposeAsync().ConfigureAwait(false);
        }

        _resolvers.Clear();
        if (_cleanupTask is not null)
        {
            await _cleanupTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private void CleanupResolvers()
    {
        lock (_lock)
        {
            if (_cleanupTask is null or { IsCompleted: true })
            {
                _cleanupTask = CleanupResolversAsyncCore();
            }
        }
    }

    private async Task CleanupResolversAsyncCore()
    {
        List<Task>? cleanupTasks = null;
        foreach (var (name, resolver) in _resolvers)
        {
            if (resolver.CanExpire() && _resolvers.TryRemove(name, out var _))
            {
                cleanupTasks ??= new();
                cleanupTasks.Add(resolver.DisposeAsync().AsTask());
            }
        }

        if (cleanupTasks is not null)
        {
            await Task.WhenAll(cleanupTasks).ConfigureAwait(false);
        }
    }

    private ResolverEntry CreateResolver(string serviceName)
    {
        var resolver = _watcherFactory.CreateWatcher(serviceName);
        resolver.Start();
        return new ResolverEntry(resolver);
    }

    private sealed class ResolverEntry(ServiceEndpointWatcher watcher) : IAsyncDisposable
    {
        private readonly ServiceEndpointWatcher _watcher = watcher;
        private const ulong CountMask = ~(RecentUseFlag | DisposingFlag);
        private const ulong RecentUseFlag = 1UL << 62;
        private const ulong DisposingFlag = 1UL << 63;
        private ulong _status;
        private TaskCompletionSource? _onDisposed;

        public string ServiceName => _watcher.ServiceName;

        public bool CanExpire()
        {
            // Read the status, clearing the recent use flag in the process.
            var status = Interlocked.And(ref _status, ~RecentUseFlag);

            // The instance can be expired if there are no concurrent callers and the recent use flag was not set.
            return (status & (CountMask | RecentUseFlag)) == 0;
        }

        public async ValueTask<(bool Valid, ServiceEndpointSource? Endpoints)> GetEndpointsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var status = Interlocked.Increment(ref _status);
                if ((status & DisposingFlag) == 0)
                {
                    // If the watcher is valid, resolve.
                    // We ensure that it will not be disposed while we are resolving.
                    var endpoints = await _watcher.GetEndpointsAsync(cancellationToken).ConfigureAwait(false);
                    return (true, endpoints);
                }
                else
                {
                    return (false, default);
                }
            }
            finally
            {
                // Set the recent use flag to prevent the instance from being disposed.
                Interlocked.Or(ref _status, RecentUseFlag);

                // If we are the last concurrent request to complete and the Disposing flag has been set,
                // dispose the resolver now. DisposeAsync was prevented by concurrent requests.
                var status = Interlocked.Decrement(ref _status);
                if ((status & DisposingFlag) == DisposingFlag && (status & CountMask) == 0)
                {
                    await DisposeAsyncCore().ConfigureAwait(false);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_onDisposed is null)
            {
                Interlocked.CompareExchange(ref _onDisposed, new(TaskCreationOptions.RunContinuationsAsynchronously), null);
            }

            var status = Interlocked.Or(ref _status, DisposingFlag);
            if ((status & DisposingFlag) != DisposingFlag && (status & CountMask) == 0)
            {
                // If we are the one who flipped the Disposing flag and there are no concurrent requests,
                // dispose the instance now. Concurrent requests are prevented from starting by the Disposing flag.
                await DisposeAsyncCore().ConfigureAwait(false);
            }
            else
            {
                await _onDisposed.Task.ConfigureAwait(false);
            }
        }

        private async Task DisposeAsyncCore()
        {
            try
            {
                await _watcher.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                Debug.Assert(_onDisposed is not null);
                _onDisposed.SetResult();
            }
        }
    }
}
