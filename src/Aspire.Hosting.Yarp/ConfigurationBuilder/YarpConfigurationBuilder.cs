// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;

namespace Aspire.Hosting;

internal class YarpConfigurationBuilder(IResourceBuilder<YarpResource> parent) : IYarpConfigurationBuilder
{
    private readonly IResourceBuilder<YarpResource> _parent = parent;

    /// <inheritdoc/>
    public YarpRoute AddRoute(string path, YarpCluster cluster)
    {
        var route = new YarpRoute(cluster);
        if (path != null)
        {
            route.WithMatchPath(path);
        }
        _parent.Resource.Routes.Add(route);
        return route;
    }

    /// <inheritdoc/>
    public YarpCluster AddCluster(EndpointReference endpoint)
    {
        var destination = new YarpCluster(endpoint);
        _parent.Resource.Destinations.Add(destination);
        _parent.WithReference(endpoint);
        return destination;
    }
}
