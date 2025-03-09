// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a service configuration within a Docker Compose file.
/// </summary>
/// <remarks>
/// This class provides methods to configure Docker Compose services through image, port mappings,
/// and environment variables. It offers a foundation for programmatically constructing Docker Compose files
/// by interacting with YAML representation objects.
/// </remarks>
public sealed class ComposeService : YamlObject
{
    /// <summary>
    /// Represents a service definition in a Docker Compose YAML file.
    /// </summary>
    public ComposeService(string image)
    {
        Add(DockerComposeYamlKeys.Image, new YamlValue(image));
        Add(DockerComposeYamlKeys.Ports, new YamlArray());
        Add(DockerComposeYamlKeys.Environment, new YamlObject());
    }

    /// <summary>
    /// Adds a port mapping to the service configuration in the Docker Compose YAML.
    /// </summary>
    /// <param name="portMapping">
    /// The port mapping in the format of "hostPort:containerPort" or "containerPort", where
    /// hostPort is the port on the host machine, and containerPort is the port inside the container.
    /// </param>
    /// <returns>
    /// Returns the modified <see cref="ComposeService"/> instance, enabling method chaining.
    /// </returns>
    public ComposeService AddPort(string portMapping)
    {
        (Get(DockerComposeYamlKeys.Ports) as YamlArray)?.Add(new YamlValue(portMapping));
        return this;
    }

    /// <summary>
    /// Adds an environment variable to the Compose service configuration.
    /// </summary>
    /// <param name="key">The name of the environment variable.</param>
    /// <param name="value">The value to assign to the environment variable.</param>
    /// <returns>The current instance of <see cref="ComposeService"/> for method chaining.</returns>
    public ComposeService AddEnvironmentVariable(string key, string value)
    {
        (Get(DockerComposeYamlKeys.Environment) as YamlObject)?.Add(key, new YamlValue(value));
        return this;
    }
}
