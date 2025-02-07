// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

internal sealed class LoopbackDnsServer : IDisposable
{
    readonly Socket _dnsSocket;
    readonly Socket _tcpSocket;

    public IPEndPoint DnsEndPoint => (IPEndPoint)_dnsSocket.LocalEndPoint!;

    public LoopbackDnsServer()
    {
        _dnsSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _dnsSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        _tcpSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _tcpSocket.Bind(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)_dnsSocket.LocalEndPoint!).Port));
        _tcpSocket.Listen();
    }

    public void Dispose()
    {
        _dnsSocket.Dispose();
        _tcpSocket.Dispose();
    }

    public void DisableTcpFallback()
    {
        _tcpSocket.Close();
    }

    private static async Task<int> ProcessRequestCore(ReadOnlyMemory<byte> message, Func<LoopbackDnsResponseBuilder, Task> action, Memory<byte> responseBuffer)
    {
        DnsDataReader reader = new DnsDataReader(message);

        if (!reader.TryReadHeader(out DnsMessageHeader header) ||
            !reader.TryReadQuestion(out var name, out var type, out var @class))
        {
            return 0;
        }

        LoopbackDnsResponseBuilder responseBuilder = new(name, type, @class);
        responseBuilder.TransactionId = header.TransactionId;
        responseBuilder.Flags = header.QueryFlags | QueryFlags.HasResponse;
        responseBuilder.ResponseCode = QueryResponseCode.NoError;

        await action(responseBuilder);

        DnsDataWriter writer = new(responseBuffer);
        if (!writer.TryWriteHeader(new DnsMessageHeader
        {
            TransactionId = responseBuilder.TransactionId,
            QueryFlags = responseBuilder.Flags,
            ResponseCode = responseBuilder.ResponseCode,
            QueryCount = (ushort)responseBuilder.Questions.Count,
            AnswerCount = (ushort)responseBuilder.Answers.Count,
            AuthorityCount = (ushort)responseBuilder.Authorities.Count,
            AdditionalRecordCount = (ushort)responseBuilder.Additionals.Count
        }))
        {
            throw new InvalidOperationException("Failed to write header");
        }

        foreach (var (questionName, questionType, questionClass) in responseBuilder.Questions)
        {
            if (!writer.TryWriteQuestion(questionName, questionType, questionClass))
            {
                throw new InvalidOperationException("Failed to write question");
            }
        }

        foreach (var answer in responseBuilder.Answers)
        {
            if (!writer.TryWriteResourceRecord(answer))
            {
                throw new InvalidOperationException("Failed to write answer");
            }
        }

        foreach (var authority in responseBuilder.Authorities)
        {
            if (!writer.TryWriteResourceRecord(authority))
            {
                throw new InvalidOperationException("Failed to write authority");
            }
        }

        foreach (var additional in responseBuilder.Additionals)
        {
            if (!writer.TryWriteResourceRecord(additional))
            {
                throw new InvalidOperationException("Failed to write additional records");
            }
        }

        return writer.Position;
    }

    public async Task ProcessUdpRequest(Func<LoopbackDnsResponseBuilder, Task> action)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            SocketReceiveFromResult result = await _dnsSocket.ReceiveFromAsync(buffer, remoteEndPoint);

            int bytesWritten = await ProcessRequestCore(buffer.AsMemory(0, result.ReceivedBytes), action, buffer.AsMemory(0, 512));

            await _dnsSocket.SendToAsync(buffer.AsMemory(0, bytesWritten), SocketFlags.None, result.RemoteEndPoint);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task ProcessTcpRequest(Func<LoopbackDnsResponseBuilder, Task> action)
    {
        using Socket tcpClient = await _tcpSocket.AcceptAsync();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        try
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            int bytesRead = 0;
            int length = -1;
            while (length < 0 || bytesRead < length + 2)
            {
                int toRead = length < 0 ? 2 : length + 2 - bytesRead;
                int read = await tcpClient.ReceiveAsync(buffer.AsMemory(bytesRead, toRead), SocketFlags.None);
                bytesRead += read;

                if (length < 0 && bytesRead >= 2)
                {
                    length = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(0, 2));
                }
            }

            int bytesWritten = await ProcessRequestCore(buffer.AsMemory(2, length), action, buffer.AsMemory(2));
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), (ushort)bytesWritten);
            await tcpClient.SendAsync(buffer.AsMemory(0, bytesWritten + 2), SocketFlags.None);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

internal sealed class LoopbackDnsResponseBuilder
{
    public LoopbackDnsResponseBuilder(string name, QueryType type, QueryClass @class)
    {
        Name = name;
        Type = type;
        Class = @class;
        Questions.Add((name, type, @class));
    }

    public ushort TransactionId { get; set; }
    public QueryFlags Flags { get; set; }
    public QueryResponseCode ResponseCode { get; set; }

    public string Name { get; }
    public QueryType Type { get; }
    public QueryClass Class { get; }

    public List<(string, QueryType, QueryClass)> Questions { get; } = new List<(string, QueryType, QueryClass)>();
    public List<DnsResourceRecord> Answers { get; } = new List<DnsResourceRecord>();
    public List<DnsResourceRecord> Authorities { get; } = new List<DnsResourceRecord>();
    public List<DnsResourceRecord> Additionals { get; } = new List<DnsResourceRecord>();
}

internal static class LoopbackDnsServerExtensions
{
    public static List<DnsResourceRecord> AddAddress(this List<DnsResourceRecord> records, string name, int ttl, IPAddress address)
    {
        QueryType type = address.AddressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA;
        records.Add(new DnsResourceRecord(name, type, QueryClass.Internet, ttl, address.GetAddressBytes()));
        return records;
    }

    public static List<DnsResourceRecord> AddCname(this List<DnsResourceRecord> records, string name, int ttl, string alias)
    {
        byte[] buff = new byte[256];
        if (!DnsPrimitives.TryWriteQName(buff, alias, out int length))
        {
            throw new InvalidOperationException("Failed to encode domain name");
        }

        records.Add(new DnsResourceRecord(name, QueryType.CNAME, QueryClass.Internet, ttl, buff.AsMemory(0, length)));
        return records;
    }

    public static List<DnsResourceRecord> AddService(this List<DnsResourceRecord> records, string name, int ttl, ushort priority, ushort weight, ushort port, string target)
    {
        byte[] buff = new byte[256];

        // https://www.rfc-editor.org/rfc/rfc2782
        if (!BinaryPrimitives.TryWriteUInt16BigEndian(buff, priority) ||
            !BinaryPrimitives.TryWriteUInt16BigEndian(buff.AsSpan(2), weight) ||
            !BinaryPrimitives.TryWriteUInt16BigEndian(buff.AsSpan(4), port) ||
            !DnsPrimitives.TryWriteQName(buff.AsSpan(6), target, out int length))
        {
            throw new InvalidOperationException("Failed to encode SRV record");
        }

        length += 6;

        records.Add(new DnsResourceRecord(name, QueryType.SRV, QueryClass.Internet, ttl, buff.AsMemory(0, length)));
        return records;
    }

    public static List<DnsResourceRecord> AddStartOfAuthority(this List<DnsResourceRecord> records, string name, int ttl, string mname, string rname, uint serial, uint refresh, uint retry, uint expire, uint minimum)
    {
        byte[] buff = new byte[256];

        // https://www.rfc-editor.org/rfc/rfc1035#section-3.3.13
        if (!DnsPrimitives.TryWriteQName(buff, mname, out int w1) ||
            !DnsPrimitives.TryWriteQName(buff.AsSpan(w1), rname, out int w2) ||
            !BinaryPrimitives.TryWriteUInt32BigEndian(buff.AsSpan(w1 + w2), serial) ||
            !BinaryPrimitives.TryWriteUInt32BigEndian(buff.AsSpan(w1 + w2 + 4), refresh) ||
            !BinaryPrimitives.TryWriteUInt32BigEndian(buff.AsSpan(w1 + w2 + 8), retry) ||
            !BinaryPrimitives.TryWriteUInt32BigEndian(buff.AsSpan(w1 + w2 + 12), expire) ||
            !BinaryPrimitives.TryWriteUInt32BigEndian(buff.AsSpan(w1 + w2 + 16), minimum))
        {
            throw new InvalidOperationException("Failed to encode SOA record");
        }

        int length = w1 + w2 + 20;

        records.Add(new DnsResourceRecord(name, QueryType.SOA, QueryClass.Internet, ttl, buff.AsMemory(0, length)));
        return records;
    }
}
