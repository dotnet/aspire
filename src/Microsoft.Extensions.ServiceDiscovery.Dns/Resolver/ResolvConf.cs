// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Net;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal static class ResolvConf
{
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    public static ResolverOptions GetOptions()
    {
        return GetOptions(new StreamReader("/etc/resolv.conf"));
    }

    public static ResolverOptions GetOptions(TextReader reader)
    {
        List<IPEndPoint> serverList = new();
        List<string> searchDomains = new();

        while (reader.ReadLine() is string line)
        {
            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (line.StartsWith("nameserver"))
            {
                if (tokens.Length >= 2 && IPAddress.TryParse(tokens[1], out IPAddress? address))
                {
                    serverList.Add(new IPEndPoint(address, 53)); // 53 is standard DNS port
                }
            }
            else if (line.StartsWith("search"))
            {
                searchDomains.AddRange(tokens.Skip(1));
            }
        }

        if (serverList.Count == 0)
        {
            throw new SocketException((int)SocketError.AddressNotAvailable);
        }

        var options = new ResolverOptions(serverList.ToArray())
        {
            SearchDomains = searchDomains.Count > 0 ? searchDomains.ToArray() : default
        };

        return options;
    }
}
