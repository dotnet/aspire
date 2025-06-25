// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal struct DnsDataReader : IDisposable
{
    public ArraySegment<byte> MessageBuffer { get; private set; }
    bool _returnToPool;
    private int _position;

    public DnsDataReader(ArraySegment<byte> buffer, bool returnToPool = false)
    {
        MessageBuffer = buffer;
        _position = 0;
        _returnToPool = returnToPool;
    }

    public bool TryReadHeader(out DnsMessageHeader header)
    {
        Debug.Assert(_position == 0);

        if (!DnsPrimitives.TryReadMessageHeader(MessageBuffer.AsSpan(), out header, out int bytesRead))
        {
            header = default;
            return false;
        }

        _position += bytesRead;
        return true;
    }

    internal bool TryReadQuestion(out EncodedDomainName name, out QueryType type, out QueryClass @class)
    {
        if (!TryReadDomainName(out name) ||
            !TryReadUInt16(out ushort typeAsInt) ||
            !TryReadUInt16(out ushort classAsInt))
        {
            type = 0;
            @class = 0;
            return false;
        }

        type = (QueryType)typeAsInt;
        @class = (QueryClass)classAsInt;
        return true;
    }

    public bool TryReadUInt16(out ushort value)
    {
        if (MessageBuffer.Count - _position < 2)
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16BigEndian(MessageBuffer.AsSpan(_position));
        _position += 2;
        return true;
    }

    public bool TryReadUInt32(out uint value)
    {
        if (MessageBuffer.Count - _position < 4)
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32BigEndian(MessageBuffer.AsSpan(_position));
        _position += 4;
        return true;
    }

    public bool TryReadResourceRecord(out DnsResourceRecord record)
    {
        if (!TryReadDomainName(out EncodedDomainName name) ||
            !TryReadUInt16(out ushort type) ||
            !TryReadUInt16(out ushort @class) ||
            !TryReadUInt32(out uint ttl) ||
            !TryReadUInt16(out ushort dataLength) ||
            MessageBuffer.Count - _position < dataLength)
        {
            record = default;
            return false;
        }

        ReadOnlyMemory<byte> data = MessageBuffer.AsMemory(_position, dataLength);
        _position += dataLength;

        record = new DnsResourceRecord(name, (QueryType)type, (QueryClass)@class, (int)ttl, data);
        return true;
    }

    public bool TryReadDomainName(out EncodedDomainName name)
    {
        if (DnsPrimitives.TryReadQName(MessageBuffer, _position, out name, out int bytesRead))
        {
            _position += bytesRead;
            return true;
        }

        return false;
    }

    public bool TryReadSpan(int length, out ReadOnlySpan<byte> name)
    {
        if (MessageBuffer.Count - _position < length)
        {
            name = default;
            return false;
        }

        name = MessageBuffer.AsSpan(_position, length);
        _position += length;
        return true;
    }

    public void Dispose()
    {
        if (_returnToPool && MessageBuffer.Array != null)
        {
            ArrayPool<byte>.Shared.Return(MessageBuffer.Array);
        }

        _returnToPool = false;
        MessageBuffer = default;
    }
}
