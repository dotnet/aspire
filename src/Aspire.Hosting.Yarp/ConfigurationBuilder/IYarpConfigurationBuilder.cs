// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;

namespace Aspire.Hosting;

/// <summary>
/// Interface to build a configuration file for YARP
/// </summary>
public interface IYarpConfigurationBuilder
{
    /// <summary>
    /// Add a new route to YARP that will target the cluster in parameter.
    /// </summary>
    /// <param name="path">The path to match for this route.</param>
    /// <param name="cluster">The target cluster for this route.</param>
    /// <returns></returns>
    public YarpRoute AddRoute(string path, YarpCluster cluster);

    /// <summary>
    /// Add a new cluster to YARP.
    /// </summary>
    /// <param name="endpoint">The endpoint used by this cluster.</param>
    /// <returns></returns>
    public YarpCluster AddCluster(EndpointReference endpoint);
}

/// <summary>
/// Collection of extensions methods for <see cref="IYarpConfigurationBuilder"/>
/// </summary>
public static class YarpConfigurationBuilderExtensions
{
    private const string CatchAllPath = "/{**catchall}";

    /// <summary>
    /// Add a new catch all route to YARP that will target the cluster in parameter.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="cluster">The target cluster for this route.</param>
    /// <returns></returns>
    public static YarpRoute AddRoute(this IYarpConfigurationBuilder builder, YarpCluster cluster)
    {
        return builder.AddRoute(CatchAllPath, cluster);
    }

    /// <summary>
    /// Add a new route to YARP that will target the cluster in parameter.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="path">The path to match for this route.</param>
    /// <param name="endpoint">The target endpoint for this route.</param>
    /// <returns></returns>
    public static YarpRoute AddRoute(this IYarpConfigurationBuilder builder, string path, EndpointReference endpoint)
    {
        var cluster = builder.AddCluster(endpoint);
        return builder.AddRoute(path, cluster);
    }

    /// <summary>
    /// Add a new catch all route to YARP that will target the cluster in parameter.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="endpoint">The target endpoint for this route.</param>
    /// <returns></returns>
    public static YarpRoute AddRoute(this IYarpConfigurationBuilder builder, EndpointReference endpoint)
    {
        return builder.AddRoute(CatchAllPath, endpoint);
    }
}
