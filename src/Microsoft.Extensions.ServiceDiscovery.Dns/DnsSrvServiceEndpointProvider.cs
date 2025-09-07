// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsSrvServiceEndpointProvider(
    ServiceEndpointQuery query,
    string srvQuery,
    string hostName,
    IOptionsMonitor<DnsSrvServiceEndpointProviderOptions> options,
    ILogger<DnsSrvServiceEndpointProvider> logger,
    IDnsResolver resolver,
    TimeProvider timeProvider) : DnsServiceEndpointProviderBase(query, logger, timeProvider), IHostNameFeature
{
    protected override double RetryBackOffFactor => options.CurrentValue.RetryBackOffFactor;

    protected override TimeSpan MinRetryPeriod => options.CurrentValue.MinRetryPeriod;

    protected override TimeSpan MaxRetryPeriod => options.CurrentValue.MaxRetryPeriod;

    protected override TimeSpan DefaultRefreshPeriod => options.CurrentValue.DefaultRefreshPeriod;

    public override string ToString() => "DNS SRV";

    string IHostNameFeature.HostName => hostName;

    protected override async Task ResolveAsyncCore()
    {
        var endpoints = new List<ServiceEndpoint>();
        var ttl = DefaultRefreshPeriod;
        Log.SrvQuery(logger, ServiceName, srvQuery);

        var now = _timeProvider.GetUtcNow().DateTime;
        var result = await resolver.ResolveServiceAsync(srvQuery, cancellationToken: ShutdownToken).ConfigureAwait(false);

        foreach (var record in result)
        {
            ttl = MinTtl(now, record.ExpiresAt, ttl);

            if (record.Addresses.Length > 0)
            {
                foreach (var address in record.Addresses)
                {
                    ttl = MinTtl(now, address.ExpiresAt, ttl);
                    endpoints.Add(CreateEndpoint(new IPEndPoint(address.Address, record.Port)));
                }
            }
            else
            {
                endpoints.Add(CreateEndpoint(new DnsEndPoint(record.Target.TrimEnd('.'), record.Port)));
            }
        }

        SetResult(endpoints, ttl);

        static TimeSpan MinTtl(DateTime now, DateTime expiresAt, TimeSpan existing)
        {
            var candidate = expiresAt - now;
            return candidate < existing ? candidate : existing;
        }

        ServiceEndpoint CreateEndpoint(EndPoint endPoint)
        {
            var serviceEndpoint = ServiceEndpoint.Create(endPoint);
            serviceEndpoint.Features.Set<IServiceEndpointProvider>(this);
            if (options.CurrentValue.ShouldApplyHostNameMetadata(serviceEndpoint))
            {
                serviceEndpoint.Features.Set<IHostNameFeature>(this);
            }

            return serviceEndpoint;
        }
    }
}
