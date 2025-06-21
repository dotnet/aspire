// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests.Fuzzing;

internal sealed class WriteDomainNameRoundTripFuzzer : IFuzzer
{
    private static readonly System.Globalization.IdnMapping s_idnMapping = new();
    public void FuzzTarget(ReadOnlySpan<byte> data)
    {
        // first byte is the offset of the domain name, rest is the actual
        // (simulated) DNS message payload

        byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length * 2);

        try
        {
            string domainName = Encoding.UTF8.GetString(data);
            if (!DnsPrimitives.TryWriteQName(buffer, domainName, out int written))
            {
                return;
            }

            if (!DnsPrimitives.TryReadQName(buffer.AsMemory(0, written), 0, out EncodedDomainName name, out int read))
            {
                return;
            }

            if (read != written)
            {
                throw new InvalidOperationException($"Read {read} bytes, but wrote {written} bytes");
            }

            string readName = name.ToString();

            if (!string.Equals(s_idnMapping.GetAscii(domainName).TrimEnd('.'), readName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Domain name mismatch: {readName} != {domainName}");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}