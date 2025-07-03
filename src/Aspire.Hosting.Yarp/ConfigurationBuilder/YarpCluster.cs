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
    // Testing only
    internal YarpCluster(ClusterConfig config, object target)
    {
        ClusterConfig = config;
        Target = target;
    }

    /// <summary>
    /// Construct a new YarpCluster targeting the endpoint in parameter.
    /// </summary>
    /// <param name="endpoint">The endpoint to target.</param>
    internal YarpCluster(EndpointReference endpoint)
        : this(endpoint.Resource.Name, $"{endpoint.Scheme}://_{endpoint.EndpointName}.{endpoint.Resource.Name}")
    {
    }

    /// <summary>
    /// Construct a new YarpCluster targeting the resource in parameter.
    /// </summary>
    /// <param name="resource">The resource to target.</param>
    internal YarpCluster(IResourceWithServiceDiscovery resource)
        : this(resource.Name, BuildEndpointUri(resource))
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="YarpCluster"/> with a specified external service resource.
    /// </summary>
    /// <param name="externalService">The external service.</param>
    internal YarpCluster(ExternalServiceResource externalService)
        : this(externalService.Name, GetAddressFromExternalService(externalService))
    {
    }

    private YarpCluster(string resourceName, object target)
    {
        ClusterConfig = new()
        {
            ClusterId = $"cluster_{resourceName}",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        };
        Target = target;
    }

    internal ClusterConfig ClusterConfig { get; private set; }

    internal object Target { get; private set; }

    internal void Configure(Func<ClusterConfig, ClusterConfig> configure)
    {
        ClusterConfig = configure(ClusterConfig);
    }

    private static object BuildEndpointUri(IResourceWithServiceDiscovery resource)
    {
        var resourceName = resource.Name;

        // NOTE: This should likely fallback to other endpoints with HTTP or HTTPS schemes in cases where they don't
        //       have the default names.
        var httpsEndpoint = resource.GetEndpoint("https");
        var httpEndpoint = resource.GetEndpoint("http");

        var scheme = (httpsEndpoint.Exists, httpEndpoint.Exists) switch
        {
            (true, true) => "https+http",
            (true, false) => "https",
            (false, true) => "http",
            _ => throw new ArgumentException("Cannot find a http or https endpoint for this resource.", nameof(resource))
        };

        return $"{scheme}://{resourceName}";
    }

    private static object GetAddressFromExternalService(ExternalServiceResource externalService)
    {
        if (externalService.Uri is not null)
        {
            return externalService.Uri;
        }
        if (externalService.UrlParameter is not null)
        {
            return externalService.UrlParameter;
        }
        // This shouldn't get to here as the ExternalServiceResource should ensure the URL is a valid absolute URI.
        throw new InvalidOperationException("External service must have either a URI or a URL parameter defined.");
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
