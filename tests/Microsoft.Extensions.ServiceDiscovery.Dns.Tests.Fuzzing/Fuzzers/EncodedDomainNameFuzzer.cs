// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests.Fuzzing;

internal sealed class EncodedDomainNameFuzzer : IFuzzer
{
    public void FuzzTarget(ReadOnlySpan<byte> data)
    {
        // first byte is the offset of the domain name, rest is the actual
        // (simulated) DNS message payload

        if (data.Length < 1)
        {
            return;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            int offset = data[0];
            data.Slice(1).CopyTo(buffer);

            if (!DnsPrimitives.TryReadQName(buffer.AsMemory(0, data.Length - 1), offset, out EncodedDomainName name, out _))
            {
                return;
            }

            // the domain name should be readable
            _ = name.ToString();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

    }
}