// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal class ResolverOptions
{
    public IPEndPoint[] Servers;
    public string DefaultDomain = string.Empty;
    public string[]? SearchDomains;
    public bool UseHostsFile;

    public ResolverOptions(IPEndPoint[] servers)
    {
        Servers = servers;
    }

    public ResolverOptions(IPEndPoint server)
    {
        Servers = new IPEndPoint[] { server };
    }
}
