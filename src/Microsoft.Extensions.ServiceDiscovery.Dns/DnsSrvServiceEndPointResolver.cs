// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsSrvServiceEndPointResolver(
    string serviceName,
    string srvQuery,
    string hostName,
    IOptionsMonitor<DnsSrvServiceEndPointResolverOptions> options,
    ILogger<DnsSrvServiceEndPointResolver> logger,
    IDnsQuery dnsClient,
    TimeProvider timeProvider) : DnsServiceEndPointResolverBase(serviceName, logger, timeProvider), IHostNameFeature
{
    protected override double RetryBackOffFactor => options.CurrentValue.RetryBackOffFactor;
    protected override TimeSpan MinRetryPeriod => options.CurrentValue.MinRetryPeriod;
    protected override TimeSpan MaxRetryPeriod => options.CurrentValue.MaxRetryPeriod;
    protected override TimeSpan DefaultRefreshPeriod => options.CurrentValue.DefaultRefreshPeriod;

    public override string DisplayName => "DNS SRV";

    string IHostNameFeature.HostName => hostName;

    protected override async Task ResolveAsyncCore()
    {
        var endPoints = new List<ServiceEndPoint>();
        var ttl = DefaultRefreshPeriod;
        Log.SrvQuery(logger, ServiceName, srvQuery);
        var result = await dnsClient.QueryAsync(srvQuery, QueryType.SRV, cancellationToken: ShutdownToken).ConfigureAwait(false);
        if (result.HasError)
        {
            SetException(CreateException(srvQuery, result.ErrorMessage));
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
                endPoints.Add(CreateEndPoint(new IPEndPoint(addressRecord.Address, record.Port)));
            }
            else if (targetRecord is CNameRecord canonicalNameRecord)
            {
                endPoints.Add(CreateEndPoint(new DnsEndPoint(canonicalNameRecord.CanonicalName.Value.TrimEnd('.'), record.Port)));
            }
        }

        SetResult(endPoints, ttl);

        static TimeSpan MinTtl(DnsResourceRecord record, TimeSpan existing)
        {
            var candidate = TimeSpan.FromSeconds(record.TimeToLive);
            return candidate < existing ? candidate : existing;
        }

        InvalidOperationException CreateException(string dnsName, string errorMessage)
        {
            var msg = errorMessage switch
            {
                { Length: > 0 } => $"No DNS records were found for service {ServiceName} (DNS name: {dnsName}): {errorMessage}.",
                _ => $"No DNS records were found for service {ServiceName} (DNS name: {dnsName})."
            };
            return new InvalidOperationException(msg);
        }

        ServiceEndPoint CreateEndPoint(EndPoint endPoint)
        {
            var serviceEndPoint = ServiceEndPoint.Create(endPoint);
            serviceEndPoint.Features.Set<IServiceEndPointResolver>(this);
            if (options.CurrentValue.ApplyHostNameMetadata(serviceEndPoint))
            {
                serviceEndPoint.Features.Set<IHostNameFeature>(this);
            }

            return serviceEndPoint;
        }
    }
}
