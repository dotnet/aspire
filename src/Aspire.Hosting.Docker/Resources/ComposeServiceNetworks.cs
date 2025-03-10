// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents the collection of networks associated with a Docker Compose service in YAML format.
/// </summary>
/// <remarks>
/// This class derives from <see cref="YamlArray"/>, allowing manipulation of network definitions as an ordered collection of YAML nodes.
/// It is used to define the networks that a specific service should connect to in a Docker Compose file.
/// </remarks>
public sealed class ComposeServiceNetworks: YamlArray
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComposeServiceNetworks"/> class.
    /// </summary>
    public ComposeServiceNetworks()
    {
    }
}
