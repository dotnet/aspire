// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal struct DnsResponse
{
    public DnsMessageHeader Header { get; }
    public List<DnsResourceRecord> Answers { get; }
    public List<DnsResourceRecord> Authorities { get; }
    public List<DnsResourceRecord> Additionals { get; }
    public DateTime CreatedAt { get; }
    public DateTime Expiration { get; }

    public DnsResponse(DnsMessageHeader header, DateTime createdAt, DateTime expiration, List<DnsResourceRecord> answers, List<DnsResourceRecord> authorities, List<DnsResourceRecord> additionals)
    {
        Header = header;
        CreatedAt = createdAt;
        Expiration = expiration;
        Answers = answers;
        Authorities = authorities;
        Additionals = additionals;
    }
}
