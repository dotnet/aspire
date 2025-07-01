// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yarp;

namespace Aspire.Hosting;

internal class YarpConfigurationBuilder(IResourceBuilder<YarpResource> parent) : IYarpConfigurationBuilder
{
    private bool _hasBeenBuilt;

    private readonly IResourceBuilder<YarpResource> _parent = parent;

    internal List<YarpCluster> Clusters { get; } = new();

    internal List<YarpRoute> Routes { get; } = new();

    /// <inheritdoc/>
    public YarpRoute AddRoute(string path, YarpCluster cluster)
    {
        ThrowIfHasBeenBuilt();
        var route = new YarpRoute(cluster);
        if (path != null)
        {
            route.WithMatchPath(path);
        }
        Routes.Add(route);
        return route;
    }

    /// <inheritdoc/>
    public YarpCluster AddCluster(EndpointReference endpoint)
    {
        ThrowIfHasBeenBuilt();
        var destination = new YarpCluster(endpoint);
        Clusters.Add(destination);
        _parent.WithReference(endpoint);
        return destination;
    }

    /// <inheritdoc/>
    public YarpCluster AddCluster(IResourceBuilder<IResourceWithServiceDiscovery> resource)
    {
        ThrowIfHasBeenBuilt();
        var destination = new YarpCluster(resource.Resource);
        Clusters.Add(destination);
        _parent.WithReference(resource);
        return destination;
    }

    internal void BuildAndPopulateEnvironment()
    {
        if (_hasBeenBuilt == false)
        {
            foreach (var configurator in _parent.Resource.ConfigurationBuilderDelegates)
            {
                configurator(this);
            }
            _parent.WithEnvironment(env => YarpEnvConfigGenerator.PopulateEnvVariables(env.EnvironmentVariables, this));
            _hasBeenBuilt = true;
        }
    }

    private void ThrowIfHasBeenBuilt()
    {
        if (_hasBeenBuilt)
        {
            throw new DistributedApplicationException("YarpConfigurationBuilder has already been built.");
        }
    }
}
