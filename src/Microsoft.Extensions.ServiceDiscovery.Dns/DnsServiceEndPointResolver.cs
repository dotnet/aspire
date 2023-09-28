// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// A service end point resolver that uses DNS to resolve the service end points.
/// </summary>
internal sealed partial class DnsServiceEndPointResolver : IServiceEndPointResolver
{
    private readonly object _lock = new();
    private readonly string _serviceName;
    private readonly Stopwatch _lastRefreshTimer = new();
    private readonly IOptionsMonitor<DnsServiceEndPointResolverOptions> _options;
    private readonly ILogger<DnsServiceEndPointResolver> _logger;
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly IDnsQuery _dnsClient;
    private readonly TimeProvider _timeProvider;
    private Task _resolveTask = Task.CompletedTask;
    private ResolutionStatus _lastStatus;
    private IChangeToken? _lastChangeToken;
    private CancellationTokenSource _lastCollectionCancellation;
    private List<ServiceEndPoint>? _lastEndPointCollection;
    private readonly string _addressRecordName;
    private readonly string _srvRecordName;
    private readonly int _defaultPort;
    private TimeSpan _nextRefreshPeriod;

    /// <summary>
    /// Initializes a new <see cref="DnsServiceEndPointResolver"/> instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="addressRecordName">The name used to resolve the address of this service.</param>
    /// <param name="srvRecordName">The name used to resolve this service's SRV record in DNS.</param>
    /// <param name="defaultPort">The default port to use for endpoints.</param>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="dnsClient">The DNS client.</param>
    /// <param name="timeProvider">The time provider.</param>
    public DnsServiceEndPointResolver(
        string serviceName,
        string addressRecordName,
        string srvRecordName,
        int defaultPort,
        IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
        ILogger<DnsServiceEndPointResolver> logger,
        IDnsQuery dnsClient,
        TimeProvider timeProvider)
    {
        _serviceName = serviceName;
        _options = options;
        _logger = logger;
        _lastEndPointCollection = null;
        _addressRecordName = addressRecordName;
        _srvRecordName = srvRecordName;
        _defaultPort = defaultPort;
        _dnsClient = dnsClient;
        _nextRefreshPeriod = _options.CurrentValue.MinRetryPeriod;
        _timeProvider = timeProvider;
        var cancellation = _lastCollectionCancellation = CreateCancellationTokenSource(_options.CurrentValue.DefaultRefreshPeriod);
        _lastChangeToken = new CancellationChangeToken(cancellation.Token);
    }

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

            if (_lastChangeToken is not null)
            {
                endPoints.AddChangeToken(_lastChangeToken);
            }

            return _lastStatus;
        }
    }

    private bool ShouldRefresh() => _lastEndPointCollection is null || _lastChangeToken is { HasChanged: true } || _lastRefreshTimer.Elapsed >= _nextRefreshPeriod;

    private async Task ResolveAsyncInternal()
    {
        var endPoints = new List<ServiceEndPoint>();
        var options = _options.CurrentValue;
        var ttl = options.DefaultRefreshPeriod;
        try
        {
            if (options.UseSrvQuery)
            {
                Log.SrvQuery(_logger, _serviceName, _srvRecordName);
                var result = await _dnsClient.QueryAsync(_srvRecordName, QueryType.SRV).ConfigureAwait(false);
                if (result.HasError)
                {
                    SetException(CreateException(result.ErrorMessage), ttl);
                    return;
                }

                var lookupMapping = new Dictionary<string, DnsResourceRecord>();
                foreach (var record in result.Additionals)
                {
                    ttl = MinTtl(record, ttl);
                    lookupMapping[record.DomainName] = record;
                }

                var srvRecords = result.Answers.OfType<SrvRecord>();
                foreach (var record in srvRecords)
                {
                    if (!lookupMapping.TryGetValue(record.Target, out var targetRecord))
                    {
                        continue;
                    }

                    ttl = MinTtl(record, ttl);
                    if (targetRecord is AddressRecord addressRecord)
                    {
                        endPoints.Add(ServiceEndPoint.Create(new IPEndPoint(addressRecord.Address, record.Port)));
                    }
                    else if (targetRecord is CNameRecord canonicalNameRecord)
                    {
                        endPoints.Add(ServiceEndPoint.Create(new DnsEndPoint(canonicalNameRecord.CanonicalName.Value.TrimEnd('.'), record.Port)));
                    }
                }
            }
            else
            {
                Log.AddressQuery(_logger, _serviceName, _addressRecordName);
                var addresses = await System.Net.Dns.GetHostAddressesAsync(_addressRecordName, _disposeCancellation.Token).ConfigureAwait(false);
                foreach (var address in addresses)
                {
                    endPoints.Add(ServiceEndPoint.Create(new IPEndPoint(address, _defaultPort)));
                }

                if (endPoints.Count == 0)
                {
                    SetException(CreateException(), ttl);
                    return;
                }
            }

            SetResult(endPoints, ttl);
        }
        catch (Exception exception)
        {
            SetException(exception, ttl);
            throw;
        }

        static TimeSpan MinTtl(DnsResourceRecord record, TimeSpan existing)
        {
            var candidate = TimeSpan.FromSeconds(record.TimeToLive);
            return candidate < existing ? candidate : existing;
        }

        InvalidOperationException CreateException(string? errorMessage = null)
        {
            var msg = errorMessage switch
            {
                { Length: > 0 } => $"No DNS records were found for service {_serviceName}: {errorMessage}.",
                _ => $"No DNS records were found for service {_serviceName}."
            };
            var exception = new InvalidOperationException(msg);
            return exception;
        }
    }

    private void SetException(Exception exception, TimeSpan validityPeriod) => SetResult(endPoints: null, exception, validityPeriod);
    private void SetResult(List<ServiceEndPoint> endPoints, TimeSpan validityPeriod) => SetResult(endPoints, exception: null, validityPeriod);
    private void SetResult(List<ServiceEndPoint>? endPoints, Exception? exception, TimeSpan validityPeriod)
    {
        lock (_lock)
        {
            var options = _options.CurrentValue;
            if (exception is not null)
            {
                if (_lastStatus.Exception is null)
                {
                    _nextRefreshPeriod = options.MinRetryPeriod;
                }
                else
                {
                    var nextPeriod = TimeSpan.FromTicks((long)(_nextRefreshPeriod.Ticks * options.RetryBackOffFactor));
                    _nextRefreshPeriod = nextPeriod > options.MaxRetryPeriod ? options.MaxRetryPeriod : nextPeriod;
                }

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
            else
            {
                _lastRefreshTimer.Restart();
                _nextRefreshPeriod = options.DefaultRefreshPeriod;
                _lastStatus = ResolutionStatus.Success;
            }

            validityPeriod = validityPeriod > TimeSpan.Zero && validityPeriod < _nextRefreshPeriod ? validityPeriod : _nextRefreshPeriod;
            _lastCollectionCancellation.Cancel();
            var cancellation = _lastCollectionCancellation = CreateCancellationTokenSource(validityPeriod);
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

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _disposeCancellation.Cancel();

        if (_resolveTask is { } task)
        {
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private CancellationTokenSource CreateCancellationTokenSource(TimeSpan validityPeriod)
    {
        if (validityPeriod <= TimeSpan.Zero)
        {
            // Do not invalidate on a timer, but invalidate on refresh.
            return new CancellationTokenSource();
        }
        else
        {
            return new CancellationTokenSource(validityPeriod, _timeProvider);
        }
    }
}
