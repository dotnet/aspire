// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Represents a cluster for YARP routes
/// </summary>
public class YarpCluster
{
    /// <summary>
    /// Construct a new YarpCluster targeting the endpoint in parameter.
    /// </summary>
    /// <param name="endpoint">The endpoint to target.</param>
    public YarpCluster(EndpointReference endpoint)
        : this(endpoint.Resource.Name, $"{endpoint.Scheme}://_{endpoint.EndpointName}.{endpoint.Resource.Name}")
    {
    }

    /// <summary>
    /// Construct a new YarpCluster targeting the resource in parameter.
    /// </summary>
    /// <param name="resource">The resource to target.</param>
    public YarpCluster(IResourceBuilder<IResourceWithServiceDiscovery> resource)
        : this(resource.Resource.Name, BuildEndpointUri(resource.Resource))
    {
    }

    private YarpCluster(string resourceName, string endpointUri)
    {
        ClusterConfig = new()
        {
            ClusterId = $"cluster_{resourceName}_{Guid.NewGuid().ToString("N")}",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                { "destination1", new DestinationConfig { Address = endpointUri } }
            }
        };
    }

    internal ClusterConfig ClusterConfig { get; private set; }

    internal void Configure(Func<ClusterConfig, ClusterConfig> configure)
    {
        ClusterConfig = configure(ClusterConfig);
    }

    private static string BuildEndpointUri(IResourceWithServiceDiscovery resource)
    {
        var resourceName = resource.Name;

        var httpsEndpoint = resource.GetEndpoint("https");
        var httpEndpoint = resource.GetEndpoint("http");

        var scheme = (httpsEndpoint.Exists, httpEndpoint.Exists) switch
        {
            (true, true)  => "https+http",
            (true, false) => "https",
            (false, true) => "http",
            _ => throw new ArgumentException("Cannot find a http or https endpoint for this resource.", nameof(resource))
        };

        return $"{scheme}://{resourceName}";
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
