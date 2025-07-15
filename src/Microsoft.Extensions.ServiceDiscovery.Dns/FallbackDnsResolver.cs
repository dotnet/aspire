// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed class FallbackDnsResolver : IDnsResolver
{
    private readonly LookupClient _lookupClient;
    private readonly IOptionsMonitor<DnsServiceEndpointProviderOptions> _options;
    private readonly TimeProvider _timeProvider;

    public FallbackDnsResolver(LookupClient lookupClient, IOptionsMonitor<DnsServiceEndpointProviderOptions> options, TimeProvider timeProvider)
    {
        _lookupClient = lookupClient;
        _options = options;
        _timeProvider = timeProvider;
    }

    private TimeSpan DefaultRefreshPeriod => _options.CurrentValue.DefaultRefreshPeriod;

    public async ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, CancellationToken cancellationToken = default)
    {
        DateTime expiresAt = _timeProvider.GetUtcNow().DateTime.Add(DefaultRefreshPeriod);
        var addresses = await System.Net.Dns.GetHostAddressesAsync(name, cancellationToken).ConfigureAwait(false);

        var results = new AddressResult[addresses.Length];

        for (int i = 0; i < addresses.Length; i++)
        {
            results[i] = new AddressResult
            {
                Address = addresses[i],
                ExpiresAt = expiresAt
            };
        }

        return results;
    }

    public async ValueTask<ServiceResult[]> ResolveServiceAsync(string name, CancellationToken cancellationToken = default)
    {
        DateTime now = _timeProvider.GetUtcNow().DateTime;
        var queryResult = await _lookupClient.QueryAsync(name, DnsClient.QueryType.SRV, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (queryResult.HasError)
        {
            throw CreateException(name, queryResult.ErrorMessage);
        }

        var lookupMapping = new Dictionary<string, List<AddressResult>>();
        foreach (var record in queryResult.Additionals.OfType<AddressRecord>())
        {
            if (!lookupMapping.TryGetValue(record.DomainName, out var addresses))
            {
                addresses = new List<AddressResult>();
                lookupMapping[record.DomainName] = addresses;
            }

            addresses.Add(new AddressResult
            {
                Address = record.Address,
                ExpiresAt = now.Add(TimeSpan.FromSeconds(record.TimeToLive))
            });
        }

        var srvRecords = queryResult.Answers.OfType<SrvRecord>().ToList();

        var results = new ServiceResult[srvRecords.Count];
        for (int i = 0; i < srvRecords.Count; i++)
        {
            var record = srvRecords[i];

            results[i] = new ServiceResult
            {
                ExpiresAt = now.Add(TimeSpan.FromSeconds(record.TimeToLive)),
                Priority = record.Priority,
                Weight = record.Weight,
                Port = record.Port,
                Target = record.Target,
                Addresses = lookupMapping.TryGetValue(record.Target, out var addresses)
                    ? addresses.ToArray()
                    : Array.Empty<AddressResult>()
            };
        }

        return results;
    }

    private static InvalidOperationException CreateException(string dnsName, string errorMessage)
    {
        var msg = errorMessage switch
        {
            { Length: > 0 } => $"No DNS SRV records were found for DNS name '{dnsName}': {errorMessage}.",
            _ => $"No DNS SRV records were found for DNS name '{dnsName}'",
        };
        return new InvalidOperationException(msg);
    }
}