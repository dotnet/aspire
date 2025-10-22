// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication.Connection;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Aspire.Dashboard.Configuration;

public record EndpointInfo(string Name, BindingAddress Address, HttpProtocols? HttpProtocols, bool RequireCertificate, ConnectionType ConnectionType)
{
    public static bool TryAddEndpoint(List<EndpointInfo> configuredEndpoints, BindingAddress? address, string name, HttpProtocols? httpProtocols, bool requireCertificate, ConnectionType connectionType)
    {
        if (address != null)
        {
            configuredEndpoints.Add(new EndpointInfo(name, address, httpProtocols, requireCertificate, connectionType));
            return true;
        }

        return false;
    }

    public static IEnumerable<KeyValuePair<BindingAddress, List<EndpointInfo>>> GroupEndpointsByAddress(IEnumerable<EndpointInfo> endpoints)
    {
        var groups = new List<KeyValuePair<BindingAddress, List<EndpointInfo>>>();
        var map = new Dictionary<string, List<EndpointInfo>>();

        foreach (var endpoint in endpoints)
        {
            var address = endpoint.Address;

            if (address.Port == 0)
            {
                // Port 0 — each endpoint is its own group
                groups.Add(new KeyValuePair<BindingAddress, List<EndpointInfo>>(address, [endpoint]));
            }
            else
            {
                var key = address.ToString();

                if (!map.TryGetValue(key, out var list))
                {
                    list = [];
                    map[key] = list;
                }

                list.Add(endpoint);
            }
        }

        // Add all normal (non-zero-port) grouped endpoints
        foreach (var kvp in map)
        {
            var address = kvp.Value.First().Address;
            groups.Add(new KeyValuePair<BindingAddress, List<EndpointInfo>>(address, kvp.Value));
        }

        return groups;
    }
}
