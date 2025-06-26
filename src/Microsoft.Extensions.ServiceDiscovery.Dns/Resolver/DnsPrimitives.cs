// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal static class DnsPrimitives
{
    // Maximum length of a domain name in ASCII (excluding trailing dot)
    internal const int MaxDomainNameLength = 253;

    internal static bool TryReadMessageHeader(ReadOnlySpan<byte> buffer, out DnsMessageHeader header, out int bytesRead)
    {
        // RFC 1035 4.1.1. Header section format
        if (buffer.Length < DnsMessageHeader.HeaderLength)
        {
            header = default;
            bytesRead = 0;
            return false;
        }

        header = new DnsMessageHeader
        {
            TransactionId = BinaryPrimitives.ReadUInt16BigEndian(buffer),
            QueryFlags = (QueryFlags)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2)),
            QueryCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4)),
            AnswerCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(6)),
            AuthorityCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(8)),
            AdditionalRecordCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(10))
        };

        bytesRead = DnsMessageHeader.HeaderLength;
        return true;
    }

    internal static bool TryWriteMessageHeader(Span<byte> buffer, DnsMessageHeader header, out int bytesWritten)
    {
        // RFC 1035 4.1.1. Header section format
        if (buffer.Length < DnsMessageHeader.HeaderLength)
        {
            bytesWritten = 0;
            return false;
        }

        BinaryPrimitives.WriteUInt16BigEndian(buffer, header.TransactionId);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2), (ushort)header.QueryFlags);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(4), header.QueryCount);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(6), header.AnswerCount);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(8), header.AuthorityCount);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(10), header.AdditionalRecordCount);

        bytesWritten = DnsMessageHeader.HeaderLength;
        return true;
    }

    // https://www.rfc-editor.org/rfc/rfc1035#section-2.3.4
    // labels          63 octets or less
    // name            255 octets or less

    private static readonly SearchValues<char> s_domainNameValidChars = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.");
    private static readonly IdnMapping s_idnMapping = new IdnMapping();
    internal static bool TryWriteQName(Span<byte> destination, string name, out int written)
    {
        written = 0;

        //
        // RFC 1035 4.1.2.
        //
        //     a domain name represented as a sequence of labels, where
        //     each label consists of a length octet followed by that
        //     number of octets.  The domain name terminates with the
        //     zero length octet for the null label of the root.  Note
        //     that this field may be an odd number of octets; no
        //     padding is used.
        //
        if (!Ascii.IsValid(name))
        {
            // IDN name, apply punycode
            try
            {
                // IdnMapping performs some validation internally (such as label
                // and domain name lengths), but is more relaxed than RFC
                // 1035 (e.g. allows ~ chars), so even if this conversion does
                // not throw, we still need to perform additional validation
                name = s_idnMapping.GetAscii(name);
            }
            catch
            {
                return false;
            }
        }

        if (name.Length > MaxDomainNameLength ||
            name.AsSpan().ContainsAnyExcept(s_domainNameValidChars) ||
            destination.IsEmpty ||
            !Encoding.ASCII.TryGetBytes(name, destination.Slice(1), out int length) ||
            destination.Length < length + 2)
        {
            // buffer too small
            return false;
        }

        Span<byte> nameBuffer = destination.Slice(0, 1 + length);
        Span<byte> label;
        while (true)
        {
            // figure out the next label and prepend the length
            int index = nameBuffer.Slice(1).IndexOf((byte)'.');
            label = index == -1 ? nameBuffer.Slice(1) : nameBuffer.Slice(1, index);

            if (label.Length == 0)
            {
                // empty label (explicit root) is only allowed at the end
                if (index != -1)
                {
                    written = 0;
                    return false;
                }
            }
            // Label restrictions:
            //   - maximum 63 octets long
            //   - must start with a letter or digit (digit is allowed by RFC 1123)
            //   - may start with an underscore (underscore may be present only
            //     at the start of the label to support SRV records)
            //   - must end with a letter or digit
            else if (label.Length > 63 ||
                !char.IsAsciiLetterOrDigit((char)label[0]) && label[0] != '_' ||
                label.Slice(1).Contains((byte)'_') ||
                !char.IsAsciiLetterOrDigit((char)label[^1]))
            {
                written = 0;
                return false;
            }

            nameBuffer[0] = (byte)label.Length;
            written += label.Length + 1;

            if (index == -1)
            {
                // this was the last label
                break;
            }

            nameBuffer = nameBuffer.Slice(index + 1);
        }

        // Add root label if wasn't explicitly specified
        if (label.Length != 0)
        {
            destination[written] = 0;
            written++;
        }

        return true;
    }

    private static bool TryReadQNameCore(List<ReadOnlyMemory<byte>> labels, int totalLength, ReadOnlyMemory<byte> messageBuffer, int offset, out int bytesRead, bool canStartWithPointer = true)
    {
        //
        // domain name can be either
        // - a sequence of labels, where each label consists of a length octet
        //   followed by that number of octets, terminated by a zero length octet
        //   (root label)
        // - a pointer, where the first two bits are set to 1, and the remaining
        //   14 bits are an offset (from the start of the message) to the true
        //   label
        //
        // It is not specified by the RFC if pointers must be backwards only,
        // the code below prohibits forward (and self) pointers to avoid
        // infinite loops. It also allows pointers only to point to a
        // label, not to another pointer.
        //

        bytesRead = 0;
        bool allowPointer = canStartWithPointer;

        if (offset < 0 || offset >= messageBuffer.Length)
        {
            return false;
        }

        int currentOffset = offset;

        while (true)
        {
            byte length = messageBuffer.Span[currentOffset];

            if ((length & 0xC0) == 0x00)
            {
                // length followed by the label
                if (length == 0)
                {
                    // end of name
                    bytesRead = currentOffset - offset + 1;
                    return true;
                }

                if (currentOffset + 1 + length >= messageBuffer.Length)
                {
                    // too many labels or truncated data
                    break;
                }

                // read next label/segment
                labels.Add(messageBuffer.Slice(currentOffset + 1, length));
                totalLength += 1 + length;

                // subtract one for the length prefix of the first label
                if (totalLength - 1 > MaxDomainNameLength)
                {
                    // domain name is too long
                    return false;
                }

                currentOffset += 1 + length;
                bytesRead += 1 + length;

                // we read a label, they can be followed by pointer.
                allowPointer = true;
            }
            else if ((length & 0xC0) == 0xC0)
            {
                // pointer, together with next byte gives the offset of the true label
                if (!allowPointer || currentOffset + 1 >= messageBuffer.Length)
                {
                    // pointer to pointer or truncated data
                    break;
                }

                bytesRead += 2;
                int pointer = ((length & 0x3F) << 8) | messageBuffer.Span[currentOffset + 1];

                // we prohibit self-references and forward pointers to avoid
                // infinite loops, we do this by truncating the
                // messageBuffer at the offset where we started reading the
                // name. We also ignore the bytesRead from the recursive
                // call, as we are only interested on how many bytes we read
                // from the initial start of the name.
                return TryReadQNameCore(labels, totalLength, messageBuffer.Slice(0, offset), pointer, out int _, false);
            }
            else
            {
                // top two bits are reserved, this means invalid data
                break;
            }
        }

        return false;

    }

    internal static bool TryReadQName(ReadOnlyMemory<byte> messageBuffer, int offset, out EncodedDomainName name, out int bytesRead)
    {
        List<ReadOnlyMemory<byte>> labels = new List<ReadOnlyMemory<byte>>();

        if (TryReadQNameCore(labels, 0, messageBuffer, offset, out bytesRead))
        {
            name = new EncodedDomainName(labels);
            return true;
        }
        else
        {
            bytesRead = 0;
            name = default;
            return false;
        }
    }

    internal static bool TryReadService(ReadOnlyMemory<byte> buffer, out ushort priority, out ushort weight, out ushort port, out EncodedDomainName target, out int bytesRead)
    {
        // https://www.rfc-editor.org/rfc/rfc2782
        if (!BinaryPrimitives.TryReadUInt16BigEndian(buffer.Span, out priority) ||
            !BinaryPrimitives.TryReadUInt16BigEndian(buffer.Span.Slice(2), out weight) ||
            !BinaryPrimitives.TryReadUInt16BigEndian(buffer.Span.Slice(4), out port) ||
            !TryReadQName(buffer.Slice(6), 0, out target, out bytesRead))
        {
            target = default;
            priority = 0;
            weight = 0;
            port = 0;
            bytesRead = 0;
            return false;
        }

        bytesRead += 6;
        return true;
    }

    internal static bool TryReadSoa(ReadOnlyMemory<byte> buffer, out EncodedDomainName primaryNameServer, out EncodedDomainName responsibleMailAddress, out uint serial, out uint refresh, out uint retry, out uint expire, out uint minimum, out int bytesRead)
    {
        // https://www.rfc-editor.org/rfc/rfc1035#section-3.3.13
        if (!TryReadQName(buffer, 0, out primaryNameServer, out int w1) ||
            !TryReadQName(buffer.Slice(w1), 0, out responsibleMailAddress, out int w2) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Span.Slice(w1 + w2), out serial) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Span.Slice(w1 + w2 + 4), out refresh) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Span.Slice(w1 + w2 + 8), out retry) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Span.Slice(w1 + w2 + 12), out expire) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Span.Slice(w1 + w2 + 16), out minimum))
        {
            primaryNameServer = default;
            responsibleMailAddress = default;
            serial = 0;
            refresh = 0;
            retry = 0;
            expire = 0;
            minimum = 0;
            bytesRead = 0;
            return false;
        }

        bytesRead = w1 + w2 + 20;
        return true;
    }
}
