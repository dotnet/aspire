// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// A service end point resolver that uses DNS to resolve the service end points.
/// </summary>
internal sealed partial class DnsServiceEndPointResolver : IServiceEndPointResolver, IHostNameFeature
{
    private readonly object _lock = new();
    private readonly string _serviceName;
    private readonly IOptionsMonitor<DnsServiceEndPointResolverOptions> _options;
    private readonly ILogger<DnsServiceEndPointResolver> _logger;
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly TimeProvider _timeProvider;
    private readonly string _hostName;
    private readonly int _defaultPort;
    private long _lastRefreshTimeStamp;
    private Task _resolveTask = Task.CompletedTask;
    private ResolutionStatus _lastStatus;
    private CancellationChangeToken _lastChangeToken;
    private CancellationTokenSource _lastCollectionCancellation;
    private List<ServiceEndPoint>? _lastEndPointCollection;
    private TimeSpan _nextRefreshPeriod;

    /// <summary>
    /// Initializes a new <see cref="DnsServiceEndPointResolver"/> instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="hostName">The name used to resolve the address of this service.</param>
    /// <param name="defaultPort">The default port to use for endpoints.</param>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public DnsServiceEndPointResolver(
        string serviceName,
        string hostName,
        int defaultPort,
        IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
        ILogger<DnsServiceEndPointResolver> logger,
        TimeProvider timeProvider)
    {
        _serviceName = serviceName;
        _options = options;
        _logger = logger;
        _lastEndPointCollection = null;
        _hostName = hostName;
        _defaultPort = defaultPort;
        _nextRefreshPeriod = _options.CurrentValue.MinRetryPeriod;
        _timeProvider = timeProvider;
        _lastRefreshTimeStamp = _timeProvider.GetTimestamp();
        var cancellation = _lastCollectionCancellation = new CancellationTokenSource();
        _lastChangeToken = new CancellationChangeToken(cancellation.Token);
    }

    private TimeSpan ElapsedSinceRefresh => _timeProvider.GetElapsedTime(_lastRefreshTimeStamp);

    string IHostNameFeature.HostName => _hostName;

    /// <inheritdoc/>
    public async ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken)
    {
        // Only add endpoints to the collection if a previous provider (eg, a configuration override) did not add them.
        if (endPoints.EndPoints.Count != 0)
        {
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

    private async Task ResolveAsyncInternal()
    {
        var endPoints = new List<ServiceEndPoint>();
        var options = _options.CurrentValue;
        var ttl = options.DefaultRefreshPeriod;
        try
        {
            Log.AddressQuery(_logger, _serviceName, _hostName);
            var addresses = await System.Net.Dns.GetHostAddressesAsync(_hostName, _disposeCancellation.Token).ConfigureAwait(false);
            foreach (var address in addresses)
            {
                endPoints.Add(CreateEndPoint(new IPEndPoint(address, _defaultPort)));
            }

            if (endPoints.Count == 0)
            {
                SetException(CreateException(_hostName));
                return;
            }

            SetResult(endPoints, ttl);
        }
        catch (Exception exception)
        {
            SetException(exception);
            throw;
        }

        InvalidOperationException CreateException(string dnsName, string? errorMessage = null)
        {
            var msg = errorMessage switch
            {
                { Length: > 0 } => $"No DNS records were found for service {_serviceName} (DNS name: {dnsName}): {errorMessage}.",
                _ => $"No DNS records were found for service {_serviceName} (DNS name: {dnsName})."
            };
            var exception = new InvalidOperationException(msg);
            return exception;
        }
    }

    private ServiceEndPoint CreateEndPoint(EndPoint endPoint)
    {
        var serviceEndPoint = ServiceEndPoint.Create(endPoint);
        serviceEndPoint.Features.Set<IHostNameFeature>(this);
        return serviceEndPoint;
    }

    private void SetException(Exception exception) => SetResult(endPoints: null, exception, validityPeriod: TimeSpan.Zero);
    private void SetResult(List<ServiceEndPoint> endPoints, TimeSpan validityPeriod) => SetResult(endPoints, exception: null, validityPeriod);
    private void SetResult(List<ServiceEndPoint>? endPoints, Exception? exception, TimeSpan validityPeriod)
    {
        lock (_lock)
        {
            var options = _options.CurrentValue;
            if (exception is not null)
            {
                _nextRefreshPeriod = CalculateNextRefreshPeriod(_lastStatus.StatusCode, options, _nextRefreshPeriod);
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
                _nextRefreshPeriod = CalculateNextRefreshPeriod(_lastStatus.StatusCode, options, _nextRefreshPeriod);
                validityPeriod = TimeSpan.Zero;
                _lastStatus = ResolutionStatus.Pending;
            }
            else
            {
                _lastRefreshTimeStamp = _timeProvider.GetTimestamp();
                _nextRefreshPeriod = options.DefaultRefreshPeriod;
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

        if (exception is null)
        {
            Debug.Assert(endPoints is not null);
            Log.DiscoveredEndPoints(_logger, endPoints, _serviceName, validityPeriod);
        }
        else
        {
            Log.ResolutionFailed(_logger, exception, _serviceName);
        }
    }

    private static TimeSpan CalculateNextRefreshPeriod(ResolutionStatusCode statusCode, DnsServiceEndPointResolverOptions options, TimeSpan currentNextRefreshPeriod)
    {
        if (statusCode is ResolutionStatusCode.Success)
        {
            return options.MinRetryPeriod;
        }

        var nextPeriod = TimeSpan.FromTicks((long)(currentNextRefreshPeriod.Ticks * options.RetryBackOffFactor));
        return nextPeriod > options.MaxRetryPeriod ? options.MaxRetryPeriod : nextPeriod;
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
