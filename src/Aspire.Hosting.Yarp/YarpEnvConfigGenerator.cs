// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Authentication;
using Yarp.ReverseProxy.Configuration;

namespace Aspire.Hosting;

internal class YarpEnvConfigGenerator
{
    private const string Prefix = "REVERSEPROXY__";

    public static void PopulateEnvVariables(Dictionary<string, object> environmentVariables, YarpConfigurationBuilder configBuilder)
    {
        var routes = configBuilder.Routes.Select(r => r.RouteConfig).ToList();
        var clusters = configBuilder.Clusters.Select(c => c.ClusterConfig).ToList();

        PopulateEnvVariables(environmentVariables, routes, clusters);
    }

    public static void PopulateEnvVariables(Dictionary<string, object> environmentVariables, List<RouteConfig> routes, List<ClusterConfig> clusters)
    {
        foreach (var route in routes)
        {
            FlattenToEnvVars(environmentVariables, route, $"{Prefix}ROUTES__{route.RouteId}");
            // hack
            environmentVariables.Remove($"{Prefix}ROUTES__{route.RouteId}__ROUTEID");
        }

        foreach (var cluster in clusters)
        {
            FlattenToEnvVars(environmentVariables, cluster, $"{Prefix}CLUSTERS__{cluster.ClusterId}");
            // hack
            environmentVariables.Remove($"{Prefix}CLUSTERS__{cluster.ClusterId}__CLUSTERID");
        }
    }

    private static void FlattenToEnvVars(Dictionary<string, object> environmentVariables, object obj, string prefix)
    {
        if (IsSimple(obj))
        {
            environmentVariables.Add(prefix, obj);
        }
        else if (obj is IReadOnlyDictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                FlattenToEnvVars(environmentVariables, kvp.Value, $"{prefix}__{kvp.Key}");
            }
        }
        else if (obj is IReadOnlyDictionary<string, DestinationConfig> dict2)
        {
            foreach (var kvp in dict2)
            {
                FlattenToEnvVars(environmentVariables, kvp.Value, $"{prefix}__{kvp.Key}");
            }
        }
        else if (obj is IReadOnlyList<object> list)
        {
            var counter = 0;
            foreach (var subValue in list)
            {
                FlattenToEnvVars(environmentVariables, subValue, $"{prefix}__{counter}");
                counter++;
            }
        }
        else if (obj is Version version)
        {
            environmentVariables.Add(prefix, version.ToString());
        }
        else if (obj is SslProtocols sslProtocols)
        {
            var counter = 0;
            foreach (var protocol in Enum.GetValues<SslProtocols>())
            {
                if (protocol != SslProtocols.None)
                {
                    if ((sslProtocols & protocol) == protocol)
                    {
                        environmentVariables.Add($"{prefix}__{counter}", protocol);
                        counter++;
                    }
                }
            }
        }
        else
        {
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(obj);

                if (value == null)
                {
                    continue;
                }
                else
                {
                    var key = $"{prefix}__{property.Name.ToUpperInvariant()}";
                    FlattenToEnvVars(environmentVariables, value, key);
                }
            }
        }
    }

    private static bool IsSimple(object value)
    {
        var type = value.GetType();
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
    }
}
