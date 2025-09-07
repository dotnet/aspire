// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal sealed class ResolverOptions
{
    public IReadOnlyList<IPEndPoint> Servers;
    public int Attempts = 2;
    public TimeSpan Timeout = TimeSpan.FromSeconds(3);

    // override for testing purposes
    internal Func<Memory<byte>, int, int>? _transportOverride;

    public ResolverOptions(IReadOnlyList<IPEndPoint> servers)
    {
        if (servers.Count == 0)
        {
            throw new ArgumentException("At least one DNS server is required.", nameof(servers));
        }

        Servers = servers;
    }

    public ResolverOptions(IPEndPoint server)
    {
        Servers = new IPEndPoint[] { server };
    }
}
