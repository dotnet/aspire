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
    public ReadOnlyMemory<byte> RawData => _rawData ?? ReadOnlyMemory<byte>.Empty;
    private byte[]? _rawData;

    public DnsResponse(byte[]? rawData, DnsMessageHeader header, DateTime createdAt, DateTime expiration, List<DnsResourceRecord> answers, List<DnsResourceRecord> authorities, List<DnsResourceRecord> additionals)
    {
        _rawData = rawData;

        Header = header;
        CreatedAt = createdAt;
        Expiration = expiration;
        Answers = answers;
        Authorities = authorities;
        Additionals = additionals;
    }

    public void Dispose()
    {
        if (_rawData != null)
        {
            ArrayPool<byte>.Shared.Return(_rawData);
            _rawData = null;
        }
    }
}
