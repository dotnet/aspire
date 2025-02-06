// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal static class DnsPrimitives
{
    // Maximum length of a domain name in ASCII (excluding trailing dot)
    internal const int MaxDomainNameLength = 253;

    internal static bool TryWriteQName(Span<byte> destination, string name, out int written)
    {
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

        // The is assumed to be already validated and puny-encoded if needed
        Debug.Assert(name.Length <= MaxDomainNameLength);
        Debug.Assert(Ascii.IsValid(name));

        if (destination.IsEmpty || !Encoding.ASCII.TryGetBytes(name, destination.Slice(1), out int length) || destination.Length < length + 2)
        {
            // buffer too small
            written = 0;
            return false;
        }

        destination[1 + length] = 0; // last label (root)

        Span<byte> nameBuffer = destination.Slice(0, 1 + length);
        while (true)
        {
            // figure out the next label and prepend the length
            int index = nameBuffer.Slice(1).IndexOf((byte)'.');
            int labelLen = index == -1 ? nameBuffer.Length - 1 : index;

            // https://www.rfc-editor.org/rfc/rfc1035#section-2.3.4
            // labels          63 octets or less
            if (labelLen > 63)
            {
                // this should never happen, as we validate the name before calling this method
                throw new ArgumentException("Label is too long");
            }

            nameBuffer[0] = (byte)labelLen;
            if (index == -1)
            {
                // this was the last label
                break;
            }

            nameBuffer = nameBuffer.Slice(index + 1);
        }

        written = length + 2;
        return true;
    }

    private static bool TryReadQNameCore(StringBuilder sb, ReadOnlySpan<byte> messageBuffer, int offset, out int bytesRead, bool canStartWithPointer = true)
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
            byte length = messageBuffer[currentOffset];

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
                if (sb.Length > 0)
                {
                    sb.Append('.');
                }

                sb.Append(Encoding.ASCII.GetString(messageBuffer.Slice(currentOffset + 1, length)));

                if (sb.Length > MaxDomainNameLength)
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
                int pointer = ((length & 0x3F) << 8) | messageBuffer[currentOffset + 1];

                // we prohibit self-references and forward pointers to avoid
                // infinite loops, we do this by truncating the
                // messageBuffer at the offset where we started reading the
                // name. We also ignore the bytesRead from the recursive
                // call, as we are only interested on how many bytes we read
                // from the initial start of the name.
                return TryReadQNameCore(sb, messageBuffer.Slice(0, offset), pointer, out int _, false);
            }
            else
            {
                // top two bits are reserved, this means invalid data
                break;
            }
        }

        return false;

    }

    internal static bool TryReadQName(ReadOnlySpan<byte> messageBuffer, int offset, [NotNullWhen(true)] out string? name, out int bytesRead)
    {
        StringBuilder sb = new StringBuilder();

        if (TryReadQNameCore(sb, messageBuffer, offset, out bytesRead))
        {
            name = sb.ToString();
            return true;
        }
        else
        {
            bytesRead = 0;
            name = null;
            return false;
        }
    }

    internal static bool TryReadService(ReadOnlySpan<byte> buffer, out ushort priority, out ushort weight, out ushort port, [NotNullWhen(true)] out string? target, out int bytesRead)
    {
        // https://www.rfc-editor.org/rfc/rfc2782
        if (!BinaryPrimitives.TryReadUInt16BigEndian(buffer, out priority) ||
            !BinaryPrimitives.TryReadUInt16BigEndian(buffer.Slice(2), out weight) ||
            !BinaryPrimitives.TryReadUInt16BigEndian(buffer.Slice(4), out port) ||
            !TryReadQName(buffer.Slice(6), 0, out target, out bytesRead))
        {
            target = null;
            priority = 0;
            weight = 0;
            port = 0;
            bytesRead = 0;
            return false;
        }

        bytesRead += 6;
        return true;
    }

    internal static bool TryReadSoa(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out string? primaryNameServer, [NotNullWhen(true)] out string? responsibleMailAddress, out uint serial, out uint refresh, out uint retry, out uint expire, out uint minimum, out int bytesRead)
    {
        // https://www.rfc-editor.org/rfc/rfc1035#section-3.3.13
        if (!TryReadQName(buffer, 0, out primaryNameServer, out int w1) ||
            !TryReadQName(buffer.Slice(w1), 0, out responsibleMailAddress, out int w2) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Slice(w1 + w2), out serial) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Slice(w1 + w2 + 4), out refresh) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Slice(w1 + w2 + 8), out retry) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Slice(w1 + w2 + 12), out expire) ||
            !BinaryPrimitives.TryReadUInt32BigEndian(buffer.Slice(w1 + w2 + 16), out minimum))
        {
            primaryNameServer = null!;
            responsibleMailAddress = null!;
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
