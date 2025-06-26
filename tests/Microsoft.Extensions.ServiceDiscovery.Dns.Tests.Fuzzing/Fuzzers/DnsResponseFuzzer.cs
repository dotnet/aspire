// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests.Fuzzing;

internal sealed class DnsResponseFuzzer : IFuzzer
{
    DnsResolver? _resolver;
    byte[]? _buffer;
    int _length;

    public void FuzzTarget(ReadOnlySpan<byte> data)
    {
        // lazy init
        if (_resolver == null)
        {
            _buffer = new byte[4096];
            _resolver = new DnsResolver(new ResolverOptions(new IPEndPoint(IPAddress.Loopback, 53))
            {
                Timeout = TimeSpan.FromSeconds(5),
                Attempts = 1,
                _transportOverride = (buffer, length) =>
                {
                    // the first two bytes are the random transaction ID, so we keep that
                    // and use the fuzzing payload for the rest of the DNS response
                    _buffer.AsSpan(0, Math.Min(_length, buffer.Length - 2)).CopyTo(buffer.Span.Slice(2));
                    return _length + 2;
                }
            });
        }

        data.CopyTo(_buffer!);
        _length = data.Length;

        // the _transportOverride makes the execution synchronous
        ValueTask<AddressResult[]> task = _resolver!.ResolveIPAddressesAsync("www.example.com", AddressFamily.InterNetwork, CancellationToken.None);
        Debug.Assert(task.IsCompleted, "Task should be completed synchronously");
        task.GetAwaiter().GetResult();
    }
}