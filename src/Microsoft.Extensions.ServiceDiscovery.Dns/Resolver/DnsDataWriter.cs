// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal sealed class DnsDataWriter
{
    private readonly Memory<byte> _buffer;
    private int _position;

    internal DnsDataWriter(Memory<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public int Position => _position;

    internal bool TryWriteHeader(in DnsMessageHeader header)
    {
        if (!DnsPrimitives.TryWriteMessageHeader(_buffer.Span.Slice(_position), header, out int written))
        {
            return false;
        }

        _position += written;
        return true;
    }

    internal bool TryWriteQuestion(string name, QueryType type, QueryClass @class)
    {
        if (!TryWriteDomainName(name) ||
            !TryWriteUInt16((ushort)type) ||
            !TryWriteUInt16((ushort)@class))
        {
            return false;
        }

        return true;
    }

    internal bool TryWriteResourceRecord(in DnsResourceRecord record)
    {
        if (!TryWriteDomainName(record.Name) ||
            !TryWriteUInt16((ushort)record.Type) ||
            !TryWriteUInt16((ushort)record.Class) ||
            !TryWriteUInt32((uint)record.Ttl))
        {
            return false;
        }

        if (record.Data.Length + 2 > _buffer.Length - _position)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(_position), (ushort)record.Data.Length);
        _position += 2;

        record.Data.Span.CopyTo(_buffer.Span.Slice(_position));
        _position += record.Data.Length;

        return true;
    }

    internal bool TryWriteDomainName(string name)
    {
        if (DnsPrimitives.TryWriteQName(_buffer.Span.Slice(_position), name, out int written))
        {
            _position += written;
            return true;
        }

        return false;
    }

    internal bool TryWriteUInt16(ushort value)
    {
        if (_buffer.Length - _position < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt16BigEndian(_buffer.Span.Slice(_position), value);
        _position += 2;
        return true;
    }

    internal bool TryWriteUInt32(uint value)
    {
        if (_buffer.Length - _position < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt32BigEndian(_buffer.Span.Slice(_position), value);
        _position += 4;
        return true;
    }
}
