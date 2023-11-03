// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsServiceEndPointResolver(
    string serviceName,
    string hostName,
    IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
    ILogger<DnsServiceEndPointResolver> logger,
    TimeProvider timeProvider) : DnsServiceEndPointResolverBase(serviceName, logger, timeProvider), IHostNameFeature
{
    protected override double RetryBackOffFactor => options.CurrentValue.RetryBackOffFactor;
    protected override TimeSpan MinRetryPeriod => options.CurrentValue.MinRetryPeriod;
    protected override TimeSpan MaxRetryPeriod => options.CurrentValue.MaxRetryPeriod;
    protected override TimeSpan DefaultRefreshPeriod => options.CurrentValue.DefaultRefreshPeriod;

    string IHostNameFeature.HostName => hostName;

    /// <inheritdoc/>
    public override string DisplayName => "DNS";

    protected override async Task ResolveAsyncCore()
    {
        var endPoints = new List<ServiceEndPoint>();
        var ttl = DefaultRefreshPeriod;
        Log.AddressQuery(logger, ServiceName, hostName);
        var addresses = await System.Net.Dns.GetHostAddressesAsync(hostName, ShutdownToken).ConfigureAwait(false);
        foreach (var address in addresses)
        {
            var serviceEndPoint = ServiceEndPoint.Create(new IPEndPoint(address, 0));
            serviceEndPoint.Features.Set<IServiceEndPointResolver>(this);
            if (options.CurrentValue.ApplyHostNameMetadata(serviceEndPoint))
            {
                serviceEndPoint.Features.Set<IHostNameFeature>(this);
            }

            endPoints.Add(serviceEndPoint);
        }

        if (endPoints.Count == 0)
        {
            SetException(new InvalidOperationException($"No DNS records were found for service {ServiceName} (DNS name: {hostName})."));
            return;
        }

        SetResult(endPoints, ttl);
    }
}
