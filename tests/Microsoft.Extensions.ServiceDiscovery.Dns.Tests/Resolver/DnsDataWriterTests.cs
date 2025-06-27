// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class DnsDataWriterTests
{
    [Fact]
    public void WriteResourceRecord_Success()
    {
        // example A record for example.com
        byte[] expected = [
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

        DnsResourceRecord record = new DnsResourceRecord(EncodeDomainName("www.example.com"), QueryType.A, QueryClass.Internet, 3600, new byte[4]);

        byte[] buffer = new byte[512];
        DnsDataWriter writer = new DnsDataWriter(buffer);
        Assert.True(writer.TryWriteResourceRecord(record));
        Assert.Equal(expected, buffer.AsSpan().Slice(0, writer.Position).ToArray());
    }

    [Fact]
    public void WriteResourceRecord_Truncated_Fails()
    {
        // example A record for example.com
        byte[] expected = [
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

        DnsResourceRecord record = new DnsResourceRecord(EncodeDomainName("www.example.com"), QueryType.A, QueryClass.Internet, 3600, new byte[4]);

        byte[] buffer = new byte[512];
        for (int i = 0; i < expected.Length; i++)
        {
            DnsDataWriter writer = new DnsDataWriter(buffer.AsMemory(0, i));
            Assert.False(writer.TryWriteResourceRecord(record));
        }
    }

    [Fact]
    public void WriteQuestion_Success()
    {
        // example question for example.com (A record)
        byte[] expected = [
            // name (www.example.com)
            0x03, 0x77, 0x77, 0x77, 0x07, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0x03, 0x63, 0x6f, 0x6d, 0x00,
            // type (A)
            0x00, 0x01,
            // class (IN)
            0x00, 0x01
        ];

        byte[] buffer = new byte[512];
        DnsDataWriter writer = new DnsDataWriter(buffer);
        Assert.True(writer.TryWriteQuestion(EncodeDomainName("www.example.com"), QueryType.A, QueryClass.Internet));
        Assert.Equal(expected, buffer.AsSpan().Slice(0, writer.Position).ToArray());
    }

    [Fact]
    public void WriteQuestion_Truncated_Fails()
    {
        // example question for example.com (A record)
        byte[] expected = [
            // name (www.example.com)
            0x03, 0x77, 0x77, 0x77, 0x07, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0x03, 0x63, 0x6f, 0x6d, 0x00,
            // type (A)
            0x00, 0x01,
            // class (IN)
            0x00, 0x01
        ];

        byte[] buffer = new byte[512];
        for (int i = 0; i < expected.Length; i++)
        {
            DnsDataWriter writer = new DnsDataWriter(buffer.AsMemory(0, i));
            Assert.False(writer.TryWriteQuestion(EncodeDomainName("www.example.com"), QueryType.A, QueryClass.Internet));
        }
    }

    [Fact]
    public void WriteHeader_Success()
    {
        // example header
        byte[] expected = [
            // ID (0x1234)
            0x12, 0x34,
            // Flags (0x5678)
            0x56, 0x78,
            // Question count (1)
            0x00, 0x01,
            // Answer count (0)
            0x00, 0x02,
            // Authority count (0)
            0x00, 0x03,
            // Additional count (0)
            0x00, 0x04
        ];

        DnsMessageHeader header = new()
        {
            TransactionId = 0x1234,
            QueryFlags = (QueryFlags)0x5678,
            QueryCount = 1,
            AnswerCount = 2,
            AuthorityCount = 3,
            AdditionalRecordCount = 4,
        };

        byte[] buffer = new byte[512];
        DnsDataWriter writer = new DnsDataWriter(buffer);
        Assert.True(writer.TryWriteHeader(header));
        Assert.Equal(expected, buffer.AsSpan().Slice(0, writer.Position).ToArray());
    }

    private static EncodedDomainName EncodeDomainName(string name)
    {
        byte[] nameBuffer = new byte[512];
        Assert.True(DnsPrimitives.TryWriteQName(nameBuffer, name, out int nameLength));
        Assert.True(DnsPrimitives.TryReadQName(nameBuffer.AsMemory(0, nameLength), 0, out EncodedDomainName encodedDomainName, out _));
        return encodedDomainName;
    }
}
