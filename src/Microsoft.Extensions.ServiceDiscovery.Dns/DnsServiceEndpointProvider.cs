// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsServiceEndpointProvider(
    ServiceEndpointQuery query,
    string hostName,
    IOptionsMonitor<DnsServiceEndpointProviderOptions> options,
    ILogger<DnsServiceEndpointProvider> logger,
    DnsResolver resolver,
    TimeProvider timeProvider) : DnsServiceEndpointProviderBase(query, logger, timeProvider), IHostNameFeature
{
    protected override double RetryBackOffFactor => options.CurrentValue.RetryBackOffFactor;
    protected override TimeSpan MinRetryPeriod => options.CurrentValue.MinRetryPeriod;
    protected override TimeSpan MaxRetryPeriod => options.CurrentValue.MaxRetryPeriod;
    protected override TimeSpan DefaultRefreshPeriod => options.CurrentValue.DefaultRefreshPeriod;

    string IHostNameFeature.HostName => hostName;

    /// <inheritdoc/>
    public override string ToString() => "DNS";

    protected override async Task ResolveAsyncCore()
    {
        var endpoints = new List<ServiceEndpoint>();
        var ttl = DefaultRefreshPeriod;
        Log.AddressQuery(logger, ServiceName, hostName);

        var now = _timeProvider.GetUtcNow().DateTime;
        var ipv4Addresses = resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetwork, ShutdownToken).ConfigureAwait(false);
        var ipv6Addresses = resolver.ResolveIPAddressesAsync(hostName, AddressFamily.InterNetworkV6, ShutdownToken).ConfigureAwait(false);

        foreach (var address in await ipv4Addresses)
        {
            ttl = MinTtl(now, address.ExpiresAt, ttl);
            endpoints.Add(CreateEndpoint(new IPEndPoint(address.Address, 0)));
        }

        foreach (var address in await ipv6Addresses)
        {
            ttl = MinTtl(now, address.ExpiresAt, ttl);
            endpoints.Add(CreateEndpoint(new IPEndPoint(address.Address, 0)));
        }

        if (endpoints.Count == 0)
        {
            throw new InvalidOperationException($"No DNS records were found for service '{ServiceName}' (DNS name: '{hostName}').");
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
