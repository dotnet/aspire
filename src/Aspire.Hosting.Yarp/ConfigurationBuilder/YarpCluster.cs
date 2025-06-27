// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Represents a cluster for YARP routes
/// </summary>
public class YarpCluster
{
    private readonly EndpointReference? _endpoint;
    private readonly ExternalServiceResource? _externalService;

    /// <summary>
    /// Creates a new instance of <see cref="YarpCluster"/> with a specified endpoint reference.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    public YarpCluster(EndpointReference endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        ClusterConfig = CreateClusterConfig();
    }

    /// <summary>
    /// Creates a new instance of <see cref="YarpCluster"/> with a specified external service resource.
    /// </summary>
    /// <param name="externalService">The external service.</param>
    public YarpCluster(ExternalServiceResource externalService)
    {
        _externalService = externalService ?? throw new ArgumentNullException(nameof(externalService));
        ClusterConfig = CreateClusterConfig();
    }

    internal ClusterConfig ClusterConfig { get; private set; }

    internal void Configure(Func<ClusterConfig, ClusterConfig> configure)
    {
        ClusterConfig = configure(ClusterConfig);
    }

    private ClusterConfig CreateClusterConfig()
    {
        Debug.Assert(_endpoint is not null || _externalService is not null, "Either endpoint or external service must be provided.");

        var name = _endpoint?.Resource.Name ?? _externalService!.Name;
        var scheme = _endpoint?.Scheme ?? GetSchemeFromExternalService(_externalService!);

        return new ClusterConfig
        {
            ClusterId = $"cluster_{name}_{Guid.NewGuid():N}",
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
            {
                { "destination1", new DestinationConfig { Address = $"{scheme}://{name}" } },
            }
        };
    }

    private static string GetSchemeFromExternalService(ExternalServiceResource externalService)
    {
        if (externalService.Uri is not null)
        {
            return externalService.Uri.Scheme;
        }
        if (externalService.UrlParameter is not null)
        {
            var url = externalService.UrlParameter.Value;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Scheme;
            }
            // This shouldn't get to here as the ExternalServiceResource should ensure the URL is a valid absolute URI.
        }
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
