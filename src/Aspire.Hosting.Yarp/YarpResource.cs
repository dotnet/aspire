// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// A resource that represents a YARP resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class YarpResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    internal List<YarpRoute> Routes { get; } = new ();

    internal List<YarpCluster> Clusters { get; } = new ();
}
