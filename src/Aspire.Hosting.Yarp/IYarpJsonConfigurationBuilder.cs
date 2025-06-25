// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Yarp.ReverseProxy.Configuration;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Interface to build a configuration file for YARP
/// </summary>
public interface IYarpJsonConfigurationBuilder
{
    /// <summary>
    /// Add a RouteConfig to the YARP resource
    /// </summary>
    public IYarpJsonConfigurationBuilder AddRoute(RouteConfig route);

    /// <summary>
    /// Add a ClusterConfig to the YARP resource
    /// </summary>
    public IYarpJsonConfigurationBuilder AddCluster(ClusterConfig cluster);

    /// <summary>
    /// Add a YARP config to the YARP resource
    /// </summary>
    public IYarpJsonConfigurationBuilder WithConfigFile(string configFilePath);
}
