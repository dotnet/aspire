// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.NetworkInformation;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal static class NetworkInfo
{
    // basic option to get DNS serves via NetworkInfo. We may get it directly later via proper APIs.
    public static ResolverOptions GetOptions()
    {
        List<IPEndPoint> servers = new List<IPEndPoint>();

        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            IPInterfaceProperties properties = nic.GetIPProperties();
            // avoid loopback, VPN etc. Should be re-visited.

            if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet && nic.OperationalStatus == OperationalStatus.Up)
            {
                foreach (IPAddress server in properties.DnsAddresses)
                {
                    IPEndPoint ep = new IPEndPoint(server, 53); // 53 is standard DNS port
                    if (!servers.Contains(ep))
                    {
                        servers.Add(ep);
                    }
                }
            }
        }

        return new ResolverOptions(servers);
    }
}
