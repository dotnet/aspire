// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a Docker Compose file resource that imports services from a docker-compose.yml file.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="composeFilePath">The path to the docker-compose.yml file.</param>
public class DockerComposeFileResource(string name, string composeFilePath) : Resource(name)
{
    /// <summary>
    /// Gets the path to the docker-compose.yml file.
    /// </summary>
    public string ComposeFilePath { get; } = composeFilePath;

    /// <summary>
    /// Gets the mapping of service names to their container resource builders.
    /// </summary>
    internal Dictionary<string, IResourceBuilder<ContainerResource>> ServiceBuilders { get; } = new(StringComparer.OrdinalIgnoreCase);
}
