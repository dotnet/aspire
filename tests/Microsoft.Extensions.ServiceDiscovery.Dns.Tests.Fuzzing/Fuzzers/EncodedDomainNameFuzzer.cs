// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests.Fuzzing;

internal sealed class EncodedDomainNameFuzzer : IFuzzer
{
    public void FuzzTarget(ReadOnlySpan<byte> data)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            data.CopyTo(buffer);

            // attempt to read at any offset to really stress the parser
            for (int i = 0; i < data.Length; i++)
            {
                if (!DnsPrimitives.TryReadQName(buffer.AsMemory(0, data.Length), i, out EncodedDomainName name, out _))
                {
                    continue;
                }

                // the domain name should be readable
                _ = name.ToString();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

    }
}