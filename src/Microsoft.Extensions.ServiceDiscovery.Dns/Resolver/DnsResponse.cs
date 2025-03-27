// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal struct DnsResponse : IDisposable
{
    public DnsMessageHeader Header { get; }
    public List<DnsResourceRecord> Answers { get; }
    public List<DnsResourceRecord> Authorities { get; }
    public List<DnsResourceRecord> Additionals { get; }
    public DateTime CreatedAt { get; }
    public DateTime Expiration { get; }
    public ArraySegment<byte> RawMessageBytes { get; private set; }

    public DnsResponse(ArraySegment<byte> rawData, DnsMessageHeader header, DateTime createdAt, DateTime expiration, List<DnsResourceRecord> answers, List<DnsResourceRecord> authorities, List<DnsResourceRecord> additionals)
    {
        RawMessageBytes = rawData;

        Header = header;
        CreatedAt = createdAt;
        Expiration = expiration;
        Answers = answers;
        Authorities = authorities;
        Additionals = additionals;
    }

    public void Dispose()
    {
        if (RawMessageBytes.Array != null)
        {
            ArrayPool<byte>.Shared.Return(RawMessageBytes.Array);
        }

        RawMessageBytes = default; // prevent further access to the raw data
    }
}
