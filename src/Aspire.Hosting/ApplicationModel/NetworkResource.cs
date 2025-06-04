// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a network resource.
/// </summary>
public class NetworkResource : Resource, IResourceWithoutLifetime
{
    /// <summary>
    /// Initializes a new instance of <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="name">The name of the network resource.</param>
    public NetworkResource(string name) : base(name)
    {
    }
}
