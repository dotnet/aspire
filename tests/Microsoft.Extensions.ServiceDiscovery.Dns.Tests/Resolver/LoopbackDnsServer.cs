// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

internal sealed class LoopbackDnsServer : IDisposable
{
    private readonly Socket _dnsSocket;
    private Socket? _tcpSocket;

    public IPEndPoint DnsEndPoint => (IPEndPoint)_dnsSocket.LocalEndPoint!;

    public LoopbackDnsServer()
    {
        _dnsSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _dnsSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
    }

    public void Dispose()
    {
        _dnsSocket.Dispose();
        _tcpSocket?.Dispose();
    }

    private static async Task<int> ProcessRequestCore(IPEndPoint remoteEndPoint, ArraySegment<byte> message, Func<LoopbackDnsResponseBuilder, IPEndPoint, Task> action, Memory<byte> responseBuffer)
    {
        DnsDataReader reader = new DnsDataReader(message);

        if (!reader.TryReadHeader(out DnsMessageHeader header) ||
            !reader.TryReadQuestion(out var name, out var type, out var @class))
        {
            return 0;
        }

        LoopbackDnsResponseBuilder responseBuilder = new(name.ToString(), type, @class);
        responseBuilder.TransactionId = header.TransactionId;
        responseBuilder.Flags = header.QueryFlags | QueryFlags.HasResponse;
        responseBuilder.ResponseCode = QueryResponseCode.NoError;

        await action(responseBuilder, remoteEndPoint);

        return responseBuilder.Write(responseBuffer);
    }

    public async Task ProcessUdpRequest(Func<LoopbackDnsResponseBuilder, IPEndPoint, Task> action)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            SocketReceiveFromResult result = await _dnsSocket.ReceiveFromAsync(buffer, remoteEndPoint);

            int bytesWritten = await ProcessRequestCore((IPEndPoint)result.RemoteEndPoint, new ArraySegment<byte>(buffer, 0, result.ReceivedBytes), action, buffer.AsMemory(0, 512));

            await _dnsSocket.SendToAsync(buffer.AsMemory(0, bytesWritten), SocketFlags.None, result.RemoteEndPoint);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public Task ProcessUdpRequest(Func<LoopbackDnsResponseBuilder, Task> action)
    {
        return ProcessUdpRequest((builder, _) => action(builder));
    }

    public async Task ProcessTcpRequest(Func<LoopbackDnsResponseBuilder, IPEndPoint, Task> action)
    {
        if (_tcpSocket is null)
        {
            _tcpSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _tcpSocket.Bind(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)_dnsSocket.LocalEndPoint!).Port));
            _tcpSocket.Listen();
        }

        using Socket tcpClient = await _tcpSocket.AcceptAsync();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        try
        {
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

            int bytesWritten = await ProcessRequestCore((IPEndPoint)tcpClient.RemoteEndPoint!, new ArraySegment<byte>(buffer, 2, length), action, buffer.AsMemory(2));
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), (ushort)bytesWritten);
            await tcpClient.SendAsync(buffer.AsMemory(0, bytesWritten + 2), SocketFlags.None);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public Task ProcessTcpRequest(Func<LoopbackDnsResponseBuilder, Task> action)
    {
        return ProcessTcpRequest((builder, _) => action(builder));
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

    public int Write(Memory<byte> responseBuffer)
    {
        DnsDataWriter writer = new(responseBuffer);
        if (!writer.TryWriteHeader(new DnsMessageHeader
        {
            TransactionId = TransactionId,
            QueryFlags = Flags | (QueryFlags)ResponseCode,
            QueryCount = (ushort)Questions.Count,
            AnswerCount = (ushort)Answers.Count,
            AuthorityCount = (ushort)Authorities.Count,
            AdditionalRecordCount = (ushort)Additionals.Count
        }))
        {
            throw new InvalidOperationException("Failed to write header");
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
        foreach (var (questionName, questionType, questionClass) in Questions)
        {
            if (!DnsPrimitives.TryWriteQName(buffer, questionName, out int length) ||
                !DnsPrimitives.TryReadQName(buffer.AsMemory(0, length), 0, out EncodedDomainName encodedName, out _))
            {
                throw new InvalidOperationException("Failed to encode domain name");
            }
            if (!writer.TryWriteQuestion(encodedName, questionType, questionClass))
            {
                throw new InvalidOperationException("Failed to write question");
            }
        }
        ArrayPool<byte>.Shared.Return(buffer);

        foreach (var answer in Answers)
        {
            if (!writer.TryWriteResourceRecord(answer))
            {
                throw new InvalidOperationException("Failed to write answer");
            }
        }

        foreach (var authority in Authorities)
        {
            if (!writer.TryWriteResourceRecord(authority))
            {
                throw new InvalidOperationException("Failed to write authority");
            }
        }

        foreach (var additional in Additionals)
        {
            if (!writer.TryWriteResourceRecord(additional))
            {
                throw new InvalidOperationException("Failed to write additional records");
            }
        }

        return writer.Position;
    }

    public byte[] GetMessageBytes()
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            int bytesWritten = Write(buffer.AsMemory(0, 512));
            return buffer.AsSpan(0, bytesWritten).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

internal static class LoopbackDnsServerExtensions
{
    private static readonly IdnMapping s_idnMapping = new IdnMapping();

    private static EncodedDomainName EncodeDomainName(string name)
    {
        var encodedLabels = name.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(label => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(s_idnMapping.GetAscii(label)))
            .ToList();

        return new EncodedDomainName(encodedLabels);
    }

    public static List<DnsResourceRecord> AddAddress(this List<DnsResourceRecord> records, string name, int ttl, IPAddress address)
    {
        QueryType type = address.AddressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA;
        records.Add(new DnsResourceRecord(EncodeDomainName(name), type, QueryClass.Internet, ttl, address.GetAddressBytes()));
        return records;
    }

    public static List<DnsResourceRecord> AddCname(this List<DnsResourceRecord> records, string name, int ttl, string alias)
    {
        byte[] buff = new byte[256];
        if (!DnsPrimitives.TryWriteQName(buff, alias, out int length))
        {
            throw new InvalidOperationException("Failed to encode domain name");
        }

        records.Add(new DnsResourceRecord(EncodeDomainName(name), QueryType.CNAME, QueryClass.Internet, ttl, buff.AsMemory(0, length)));
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

        records.Add(new DnsResourceRecord(EncodeDomainName(name), QueryType.SRV, QueryClass.Internet, ttl, buff.AsMemory(0, length)));
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

        records.Add(new DnsResourceRecord(EncodeDomainName(name), QueryType.SOA, QueryClass.Internet, ttl, buff.AsMemory(0, length)));
        return records;
    }
}

internal static class DnsDataWriterExtensions
{
    internal static bool TryWriteResourceRecord(this DnsDataWriter writer, DnsResourceRecord record)
    {
        if (!TryWriteDomainName(writer, record.Name) ||
            !writer.TryWriteUInt16((ushort)record.Type) ||
            !writer.TryWriteUInt16((ushort)record.Class) ||
            !writer.TryWriteUInt32((uint)record.Ttl) ||
            !writer.TryWriteUInt16((ushort)record.Data.Length) ||
            !writer.TryWriteRawData(record.Data.Span))
        {
            return false;
        }

        return true;
    }

    internal static bool TryWriteDomainName(this DnsDataWriter writer, EncodedDomainName name)
    {
        foreach (var label in name.Labels)
        {
            if (label.Length > 63)
            {
                throw new InvalidOperationException("Label length exceeds maximum of 63 bytes");
            }

            if (!writer.TryWriteByte((byte)label.Length) ||
                !writer.TryWriteRawData(label.Span))
            {
                return false;
            }
        }

        // root label
        return writer.TryWriteByte(0);
    }
}