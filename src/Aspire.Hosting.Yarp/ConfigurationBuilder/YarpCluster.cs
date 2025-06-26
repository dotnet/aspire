// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Represents a cluster for YARP routes
/// </summary>
public class YarpCluster(EndpointReference endpoint)
{
    internal ClusterConfig ClusterConfig { get; private set; } = new()
    {
        ClusterId = $"cluster_{endpoint.Resource.Name}_{Guid.NewGuid().ToString("N")}",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { "destination1", new DestinationConfig { Address = $"{endpoint.Scheme}://_{endpoint.EndpointName}.{endpoint.Resource.Name}" } },
        }
    };

    internal void Configure(Func<ClusterConfig, ClusterConfig> configure)
    {
        ClusterConfig = configure(ClusterConfig);
    }
}

/// <summary>
/// Provides extension methods for configuring a YARP cluster.
/// </summary>
public static class YarpClusterExtensions
{
    /// <summary>
    /// Set the ForwarderRequestConfig for the cluster.
    /// </summary>
    public static YarpCluster WithForwarderRequestConfig(this YarpCluster cluster, ForwarderRequestConfig config)
    {
        cluster.Configure(c => c with { HttpRequest = config });
        return cluster;
    }

    /// <summary>
    /// Set the ForwarderRequestConfig for the cluster.
    /// </summary>
    public static YarpCluster WithHttpClientConfig(this YarpCluster cluster, HttpClientConfig config)
    {
        cluster.Configure(c => c with { HttpClient = config });
        return cluster;
    }

    /// <summary>
    /// Set the SessionAffinityConfig for the cluster.
    /// </summary>
    public static YarpCluster WithSessionAffinityConfig(this YarpCluster cluster, SessionAffinityConfig config)
    {
        cluster.Configure(c => c with { SessionAffinity = config });
        return cluster;
    }

    /// <summary>
    /// Set the HealthCheckConfig for the cluster.
    /// </summary>
    public static YarpCluster WithHealthCheckConfig(this YarpCluster cluster, HealthCheckConfig config)
    {
        cluster.Configure(c => c with { HealthCheck = config });
        return cluster;
    }

    /// <summary>
    /// Set the LoadBalancingPolicy for the cluster.
    /// </summary>
    public static YarpCluster WithLoadBalancingPolicy(this YarpCluster cluster, string policy)
    {
        cluster.Configure(c => c with { LoadBalancingPolicy = policy });
        return cluster;
    }

    /// <summary>
    /// Set the Metadata for the cluster.
    /// </summary>
    public static YarpCluster WithMetadata(this YarpCluster cluster, IReadOnlyDictionary<string, string> metadata)
    {
        cluster.Configure(c => c with { Metadata = metadata });
        return cluster;
    }
}
