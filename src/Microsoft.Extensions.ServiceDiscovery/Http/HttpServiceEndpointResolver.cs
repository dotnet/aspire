// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery.LoadBalancing;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

/// <summary>
/// Resolves endpoints for HTTP requests.
/// </summary>
internal sealed class HttpServiceEndpointResolver(ServiceEndpointWatcherFactory watcherFactory, IServiceProvider serviceProvider, TimeProvider timeProvider) : IAsyncDisposable
{
    private static readonly TimerCallback s_cleanupCallback = s => ((HttpServiceEndpointResolver)s!).CleanupResolvers();
    private static readonly TimeSpan s_cleanupPeriod = TimeSpan.FromSeconds(10);

    private readonly object _lock = new();
    private readonly ServiceEndpointWatcherFactory _watcherFactory = watcherFactory;
    private readonly ConcurrentDictionary<string, ResolverEntry> _resolvers = new();
    private ITimer? _cleanupTimer;
    private Task? _cleanupTask;

    /// <summary>
    /// Resolves and returns a service endpoint for the specified request.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The resolved service endpoint.</returns>
    /// <exception cref="InvalidOperationException">The request had no <see cref="HttpRequestMessage.RequestUri"/> set or a suitable endpoint could not be found.</exception>
    public async ValueTask<ServiceEndpoint> GetEndpointAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestUri is null)
        {
            throw new InvalidOperationException("Cannot resolve an endpoint for a request which has no RequestUri");
        }

        EnsureCleanupTimerStarted();

        var key = request.RequestUri.GetLeftPart(UriPartial.Authority);
        while (true)
        {
            var resolver = _resolvers.GetOrAdd(
                key,
                static (name, self) => self.CreateResolver(name),
                this);

            var (valid, endpoint) = await resolver.TryGetEndpointAsync(request, cancellationToken).ConfigureAwait(false);
            if (valid)
            {
                if (endpoint is null)
                {
                    throw new InvalidOperationException($"Unable to resolve endpoint for service {resolver.ServiceName}");
                }

                return endpoint;
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

                _cleanupTimer = timeProvider.CreateTimer(s_cleanupCallback, this, s_cleanupPeriod, s_cleanupPeriod);
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
        var watcher = _watcherFactory.CreateWatcher(serviceName);
        var selector = serviceProvider.GetService<IServiceEndpointSelector>() ?? new RoundRobinServiceEndpointSelector();
        var result = new ResolverEntry(watcher, selector);
        watcher.Start();
        return result;
    }

    private sealed class ResolverEntry : IAsyncDisposable
    {
        private readonly ServiceEndpointWatcher _watcher;
        private readonly IServiceEndpointSelector _selector;
        private const ulong CountMask = ~(RecentUseFlag | DisposingFlag);
        private const ulong RecentUseFlag = 1UL << 62;
        private const ulong DisposingFlag = 1UL << 63;
        private ulong _status;
        private TaskCompletionSource? _onDisposed;

        public ResolverEntry(ServiceEndpointWatcher watcher, IServiceEndpointSelector selector)
        {
            _watcher = watcher;
            _selector = selector;
            _watcher.OnEndpointsUpdated += result =>
            {
                if (result.ResolvedSuccessfully)
                {
                    _selector.SetEndpoints(result.EndpointSource);
                }
            };
        }

        public string ServiceName => _watcher.ServiceName;

        public bool CanExpire()
        {
            // Read the status, clearing the recent use flag in the process.
            var status = Interlocked.And(ref _status, ~RecentUseFlag);

            // The instance can be expired if there are no concurrent callers and the recent use flag was not set.
            return (status & (CountMask | RecentUseFlag)) == 0;
        }

        public async ValueTask<(bool Valid, ServiceEndpoint? Endpoint)> TryGetEndpointAsync(object? context, CancellationToken cancellationToken)
        {
            try
            {
                var status = Interlocked.Increment(ref _status);
                if ((status & DisposingFlag) == 0)
                {
                    // If the watcher is valid, resolve.
                    // We ensure that it will not be disposed while we are resolving.
                    await _watcher.GetEndpointsAsync(cancellationToken).ConfigureAwait(false);
                    var result = _selector.GetEndpoint(context);
                    return (true, result);
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
