// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class DnsDataReaderTests
{
    [Fact]
    public void ReadResourceRecord_Success()
    {
        // example A record for example.com
        byte[] buffer = [
            // name (www.example.com)
            0x03, 0x77, 0x77, 0x77, 0x07, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0x03, 0x63, 0x6f, 0x6d, 0x00,
            // type (A)
            0x00, 0x01,
            // class (IN)
            0x00, 0x01,
            // TTL (3600)
            0x00, 0x00, 0x0e, 0x10,
            // data length (4)
            0x00, 0x04,
            // data (placeholder)
            0x00, 0x00, 0x00, 0x00
        ];

        DnsDataReader reader = new DnsDataReader(buffer);
        Assert.True(reader.TryReadResourceRecord(out DnsResourceRecord record));

        Assert.Equal("www.example.com", record.Name.ToString());
        Assert.Equal(QueryType.A, record.Type);
        Assert.Equal(QueryClass.Internet, record.Class);
        Assert.Equal(3600, record.Ttl);
        Assert.Equal(4, record.Data.Length);
    }

    [Fact]
    public void ReadResourceRecord_Truncated_Fails()
    {
        // example A record for example.com
        byte[] buffer = [
            // name (www.example.com)
            0x03, 0x77, 0x77, 0x77, 0x07, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0x03, 0x63, 0x6f, 0x6d, 0x00,
            // type (A)
            0x00, 0x01,
            // class (IN)
            0x00, 0x01,
            // TTL (3600)
            0x00, 0x00, 0x0e, 0x10,
            // data length (4)
            0x00, 0x04,
            // data (placeholder)
            0x00, 0x00, 0x00, 0x00
        ];

        for (int i = 0; i < buffer.Length; i++)
        {
            DnsDataReader reader = new DnsDataReader(new ArraySegment<byte>(buffer, 0, i));
            Assert.False(reader.TryReadResourceRecord(out _));
        }
    }
}
