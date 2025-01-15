// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

// RFC 1035 4.1.1. Header section format
internal struct DnsMessageHeader
{
    internal const int HeaderLength = 12;

    private ushort _transactionId;
    private ushort _flags;

    private ushort _queryCount;
    private ushort _answerCount;
    private ushort _authorityCount;
    private ushort _additionalRecordCount;

    internal ushort QueryCount
    {
        get => ReverseByteOrder(_queryCount);
        set => _queryCount = ReverseByteOrder(value);
    }

    internal ushort AnswerCount
    {
        get => ReverseByteOrder(_answerCount);
        set => _answerCount = ReverseByteOrder(value);
    }

    internal ushort AuthorityCount
    {
        get => ReverseByteOrder(_authorityCount);
        set => _authorityCount = ReverseByteOrder(value);
    }

    internal ushort AdditionalRecordCount
    {
        get => ReverseByteOrder(_additionalRecordCount);
        set => _additionalRecordCount = ReverseByteOrder(value);
    }

    internal ushort TransactionId
    {
        get => ReverseByteOrder(_transactionId);
        set => _transactionId = ReverseByteOrder(value);
    }

    internal QueryFlags QueryFlags
    {
        get => (QueryFlags)ReverseByteOrder(_flags);
        set => _flags = ReverseByteOrder((ushort)value);
    }

    internal bool IsRecursionDesired
    {
        get => (QueryFlags & QueryFlags.RecursionDesired) != 0;
        set
        {
            if (value)
            {
                QueryFlags |= QueryFlags.RecursionDesired;
            }
            else
            {
                QueryFlags &= ~QueryFlags.RecursionDesired;
            }
        }
    }

    internal QueryResponseCode ResponseCode
    {
        get => (QueryResponseCode)((_flags & 0x0F00) >> 8);
        set => _flags = (ushort)((_flags & 0xF0FF) | ((ushort)value << 8));
    }

    internal bool IsResultTruncated => (QueryFlags & QueryFlags.ResultTruncated) != 0;

    internal bool IsResponse
    {
        get => (QueryFlags & QueryFlags.HasResponse) != 0;
        set
        {
            if (value)
            {
                QueryFlags |= QueryFlags.HasResponse;
            }
            else
            {
                QueryFlags &= ~QueryFlags.HasResponse;
            }
        }
    }

    internal void InitQueryHeader()
    {
        this = default;
        TransactionId = (ushort)RandomNumberGenerator.GetInt32(short.MaxValue + 1);
        IsRecursionDesired = true;
        QueryCount = 1;
    }

    private static ushort ReverseByteOrder(ushort value) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
}
