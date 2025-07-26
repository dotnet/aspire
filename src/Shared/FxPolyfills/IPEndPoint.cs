// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

using System.Globalization;

namespace System.Net;

internal static partial class FxPolyfillIPEndPoint
{
    extension(IPEndPoint)
    {
        public static IPEndPoint Parse(string endpoint)
        {
            if (TryParse(endpoint.AsSpan(), out var result))
            {
                return result;
            }

            throw new FormatException("The endpoint format is invalid.");
        }

        public static bool TryParse(ReadOnlySpan<char> s, out IPEndPoint? result)
        {
            const int MaxPort = 0x0000FFFF;

            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf(':');

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == ']')
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Slice(0, lastColonPos).LastIndexOf(':') == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            if (IPAddress.TryParse(s.Slice(0, addressLength).ToString(), out IPAddress? address))
            {
                uint port = 0;
                if (addressLength == s.Length ||
                    (uint.TryParse(s.Slice(addressLength + 1).ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort))

                {
                    result = new IPEndPoint(address, (int)port);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}

#endif
