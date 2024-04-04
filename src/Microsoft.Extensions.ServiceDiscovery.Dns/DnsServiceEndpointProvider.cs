// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsServiceEndpointProvider(
    ServiceEndpointQuery query,
    string hostName,
    IOptionsMonitor<DnsServiceEndpointProviderOptions> options,
    ILogger<DnsServiceEndpointProvider> logger,
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
        var addresses = await System.Net.Dns.GetHostAddressesAsync(hostName, ShutdownToken).ConfigureAwait(false);
        foreach (var address in addresses)
        {
            var serviceEndpoint = ServiceEndpoint.Create(new IPEndPoint(address, 0));
            serviceEndpoint.Features.Set<IServiceEndpointProvider>(this);
            if (options.CurrentValue.ShouldApplyHostNameMetadata(serviceEndpoint))
            {
                serviceEndpoint.Features.Set<IHostNameFeature>(this);
            }

            endpoints.Add(serviceEndpoint);
        }

        if (endpoints.Count == 0)
        {
            throw new InvalidOperationException($"No DNS records were found for service '{ServiceName}' (DNS name: '{hostName}').");
        }

        SetResult(endpoints, ttl);
    }
}
