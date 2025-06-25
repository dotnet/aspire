// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

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

    internal bool TryWriteQuestion(EncodedDomainName name, QueryType type, QueryClass @class)
    {
        if (!TryWriteDomainName(name) ||
            !TryWriteUInt16((ushort)type) ||
            !TryWriteUInt16((ushort)@class))
        {
            return false;
        }

        return true;
    }

    private bool TryWriteDomainName(EncodedDomainName name)
    {
        foreach (var label in name.Labels)
        {
            // this should be already validated by the caller
            Debug.Assert(label.Length <= 63, "Label length must not exceed 63 bytes.");

            if (!TryWriteByte((byte)label.Length) ||
                !TryWriteRawData(label.Span))
            {
                return false;
            }
        }

        // root label
        return TryWriteByte(0);
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

    internal bool TryWriteByte(byte value)
    {
        if (_buffer.Length - _position < 1)
        {
            return false;
        }

        _buffer.Span[_position] = value;
        _position += 1;
        return true;
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

    internal bool TryWriteRawData(ReadOnlySpan<byte> value)
    {
        if (_buffer.Length - _position < value.Length)
        {
            return false;
        }

        value.CopyTo(_buffer.Span.Slice(_position));
        _position += value.Length;
        return true;
    }
}
