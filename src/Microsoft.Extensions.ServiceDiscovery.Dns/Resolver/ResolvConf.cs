// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        while (reader.ReadLine() is string line)
        {
            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (line.StartsWith("nameserver"))
            {
                if (tokens.Length >= 2 && IPAddress.TryParse(tokens[1], out IPAddress? address))
                {
                    serverList.Add(new IPEndPoint(address, 53)); // 53 is standard DNS port

                    if (serverList.Count == 3)
                    {
                        break; // resolv.conf manpage allow max 3 nameservers anyway
                    }
                }
            }
        }

        if (serverList.Count == 0)
        {
            // If no nameservers are configured, fall back to the default behavior of using the system resolver configuration.
            return NetworkInfo.GetOptions();
        }

        return new ResolverOptions(serverList);
    }
}
