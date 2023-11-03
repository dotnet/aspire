// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// A service end point resolver that uses DNS to resolve the service end points.
/// </summary>
internal abstract partial class DnsServiceEndPointResolverBase : IServiceEndPointResolver
{
    private readonly object _lock = new();
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly TimeProvider _timeProvider;
    private long _lastRefreshTimeStamp;
    private Task _resolveTask = Task.CompletedTask;
    private ResolutionStatus _lastStatus;
    private CancellationChangeToken _lastChangeToken;
    private CancellationTokenSource _lastCollectionCancellation;
    private List<ServiceEndPoint>? _lastEndPointCollection;
    private TimeSpan _nextRefreshPeriod;

    /// <summary>
    /// Initializes a new <see cref="DnsServiceEndPointResolverBase"/> instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    protected DnsServiceEndPointResolverBase(
        string serviceName,
        ILogger logger,
        TimeProvider timeProvider)
    {
        ServiceName = serviceName;
        _logger = logger;
        _lastEndPointCollection = null;
        _timeProvider = timeProvider;
        _lastRefreshTimeStamp = _timeProvider.GetTimestamp();
        var cancellation = _lastCollectionCancellation = new CancellationTokenSource();
        _lastChangeToken = new CancellationChangeToken(cancellation.Token);
    }

    public abstract string DisplayName { get; }

    private TimeSpan ElapsedSinceRefresh => _timeProvider.GetElapsedTime(_lastRefreshTimeStamp);

    protected string ServiceName { get; }

    protected abstract double RetryBackOffFactor { get; }
    protected abstract TimeSpan MinRetryPeriod { get; }
    protected abstract TimeSpan MaxRetryPeriod { get; }
    protected abstract TimeSpan DefaultRefreshPeriod { get; }
    protected CancellationToken ShutdownToken => _disposeCancellation.Token;

    /// <inheritdoc/>
    public async ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken)
    {
        // Only add endpoints to the collection if a previous provider (eg, a configuration override) did not add them.
        if (endPoints.EndPoints.Count != 0)
        {
            Log.SkippedResolution(_logger, ServiceName, "Collection has existing endpoints");
            return ResolutionStatus.None;
        }

        if (ShouldRefresh())
        {
            Task resolveTask;
            lock (_lock)
            {
                if (_resolveTask.IsCompleted && ShouldRefresh())
                {
                    _resolveTask = ResolveAsyncInternal();
                }

                resolveTask = _resolveTask;
            }

            await resolveTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        lock (_lock)
        {
            if (_lastEndPointCollection is { Count: > 0 } eps)
            {
                foreach (var ep in eps)
                {
                    endPoints.EndPoints.Add(ep);
                }
            }

            endPoints.AddChangeToken(_lastChangeToken);
            return _lastStatus;
        }
    }

    private bool ShouldRefresh() => _lastEndPointCollection is null || _lastChangeToken is { HasChanged: true } || ElapsedSinceRefresh >= _nextRefreshPeriod;

    protected abstract Task ResolveAsyncCore();

    private async Task ResolveAsyncInternal()
    {
        try
        {
            await ResolveAsyncCore().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            SetException(exception);
            throw;
        }

    }

    protected void SetException(Exception exception) => SetResult(endPoints: null, exception, validityPeriod: TimeSpan.Zero);
    protected void SetResult(List<ServiceEndPoint> endPoints, TimeSpan validityPeriod) => SetResult(endPoints, exception: null, validityPeriod);
    private void SetResult(List<ServiceEndPoint>? endPoints, Exception? exception, TimeSpan validityPeriod)
    {
        lock (_lock)
        {
            if (exception is not null)
            {
                _nextRefreshPeriod = GetRefreshPeriod();
                if (_lastEndPointCollection is null)
                {
                    // Since end points have never been resolved, use a pending status to indicate that they might appear
                    // soon and to retry for some period until they do.
                    _lastStatus = ResolutionStatus.FromPending(exception);
                }
                else
                {
                    _lastStatus = ResolutionStatus.FromException(exception);
                }
            }
            else if (endPoints is not { Count: > 0 })
            {
                _nextRefreshPeriod = GetRefreshPeriod();
                validityPeriod = TimeSpan.Zero;
                _lastStatus = ResolutionStatus.Pending;
            }
            else
            {
                _lastRefreshTimeStamp = _timeProvider.GetTimestamp();
                _nextRefreshPeriod = DefaultRefreshPeriod;
                _lastStatus = ResolutionStatus.Success;
            }

            if (validityPeriod <= TimeSpan.Zero)
            {
                validityPeriod = _nextRefreshPeriod;
            }
            else if (validityPeriod > _nextRefreshPeriod)
            {
                validityPeriod = _nextRefreshPeriod;
            }

            _lastCollectionCancellation.Cancel();
            var cancellation = _lastCollectionCancellation = new CancellationTokenSource(validityPeriod, _timeProvider);
            _lastChangeToken = new CancellationChangeToken(cancellation.Token);
            _lastEndPointCollection = endPoints;
        }

        TimeSpan GetRefreshPeriod()
        {
            if (_lastStatus.StatusCode is ResolutionStatusCode.Success)
            {
                return MinRetryPeriod;
            }

            var nextPeriod = TimeSpan.FromTicks((long)(_nextRefreshPeriod.Ticks * RetryBackOffFactor));
            return nextPeriod > MaxRetryPeriod ? MaxRetryPeriod : nextPeriod;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _disposeCancellation.Cancel();

        if (_resolveTask is { } task)
        {
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
