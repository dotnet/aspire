// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Yarp.ReverseProxy.Configuration;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Represents a route for YARP
/// </summary>
public class YarpRoute
{
    // Testing only
    internal YarpRoute(RouteConfig routeConfig)
    {
        RouteConfig = routeConfig;
    }

    internal YarpRoute(YarpCluster cluster, string routeId)
    {
        RouteConfig = new RouteConfig
        {
            RouteId = routeId,
            ClusterId = cluster.ClusterConfig.ClusterId,
            Match = new RouteMatch(),
        };
    }

    internal RouteConfig RouteConfig { get; private set; }

    internal void Configure(Func<RouteConfig, RouteConfig> configure)
    {
        RouteConfig = configure(RouteConfig);
    }
}

/// <summary>
/// Provides extension methods for configuring a YARP destination
/// </summary>
public static class YarpRouteExtensions
{
    /// <summary>
    /// Set the parameters used to match requests.
    /// </summary>
    public static YarpRoute WithMatch(this YarpRoute route, RouteMatch match)
    {
        route.Configure(r => r with { Match = match });
        return route;
    }

    #region RouteMatch helpers

    private static YarpRoute ConfigureMatch(this YarpRoute route, Func<RouteMatch, RouteMatch> match)
    {
        route.Configure(r => r with { Match = match(r.Match) });
        return route;
    }

    /// <summary>
    /// Only match requests with the given Path pattern.
    /// </summary>
    public static YarpRoute WithMatchPath(this YarpRoute route, string path)
    {
        route.ConfigureMatch(match => match with { Path = path });
        return route;
    }

    /// <summary>
    /// Only match requests that use these optional HTTP methods. E.g. GET, POST.
    /// </summary>
    public static YarpRoute WithMatchMethods(this YarpRoute route, params string[] methods)
    {
        route.ConfigureMatch(match => match with { Methods = methods });
        return route;
    }

    /// <summary>
    /// Only match requests that contain all of these headers.
    /// </summary>
    public static YarpRoute WithMatchHeaders(this YarpRoute route, params RouteHeader[] headers)
    {
        route.ConfigureMatch(match => match with { Headers = headers.ToList() });
        return route;
    }

    /// <summary>
    ///  Only match requests with the given Host header. Supports wildcards and ports.
    ///  For unicode host names, do not use punycode.
    /// </summary>
    public static YarpRoute WithMatchHosts(this YarpRoute route, params string[] hosts)
    {
        route.ConfigureMatch(match => match with { Hosts = hosts.ToList() });
        return route;
    }

    /// <summary>
    ///  Only match requests that contain all of these query parameters.
    /// </summary>
    public static YarpRoute WithMatchRouteQueryParameter(this YarpRoute route, params RouteQueryParameter[] queryParameters)
    {
        route.ConfigureMatch(match => match with { QueryParameters = queryParameters.ToList() });
        return route;
    }

    #endregion

    /// <summary>
    /// Set the order for the destination
    /// </summary>
    public static YarpRoute WithOrder(this YarpRoute route, int? order)
    {
        route.Configure(r => r with { Order = order });
        return route;
    }

    /// <summary>
    /// Set the MaxRequestBodySize for the destination
    /// </summary>
    public static YarpRoute WithMaxRequestBodySize(this YarpRoute route, long maxRequestBodySize)
    {
        route.Configure(r => r with { MaxRequestBodySize = maxRequestBodySize });
        return route;
    }

    /// <summary>
    /// Set the Metadata of the destination
    /// </summary>
    public static YarpRoute WithMetadata(this YarpRoute route, IReadOnlyDictionary<string, string>? metadata)
    {
        route.Configure(r => r with { Metadata = metadata });
        return route;
    }

    /// <summary>
    /// Set the Transforms of the destination
    /// </summary>
    public static YarpRoute WithTransforms(this YarpRoute route, IReadOnlyList<IReadOnlyDictionary<string, string>>? transforms)
    {
        route.Configure(r => r with { Transforms = transforms });
        return route;
    }

    /// <summary>
    /// Add a new transform to the destination
    /// </summary>
    public static YarpRoute WithTransform(this YarpRoute route, Action<IDictionary<string, string>> createTransform)
    {
        ArgumentNullException.ThrowIfNull(createTransform);

        route.Configure(r =>
        {
            List<IReadOnlyDictionary<string, string>> transforms;
            if (r.Transforms is null)
            {
                transforms = new List<IReadOnlyDictionary<string, string>>();
            }
            else
            {
                transforms = new List<IReadOnlyDictionary<string, string>>(r.Transforms.Count + 1);
                transforms.AddRange(r.Transforms);
            }

            var transform = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            createTransform(transform);
            transforms.Add(transform);

            return r with { Transforms = transforms };
        });

        return route;
    }
}
