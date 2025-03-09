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
    public ComposeService(string name)
    {
        Add(DockerComposeYamlKeys.ContainerName, new YamlValue(name));
    }

    /// <summary>
    /// Gets the name of the Docker Compose service.
    /// </summary>
    /// <remarks>
    /// This property retrieves the container name specified in the Docker Compose configuration.
    /// If the name is not explicitly set, it returns an empty string.
    /// </remarks>
    public string? Name => Get(DockerComposeYamlKeys.ContainerName) is YamlValue name ?
        name.Value.ToString() :
        throw new DistributedApplicationException("Container name not set.");

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
        var ports = GetOrCreate<YamlArray>(DockerComposeYamlKeys.Ports);
        ports.Add(new YamlValue(portMapping));
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
        var env = GetOrCreate<YamlObject>(DockerComposeYamlKeys.Environment);
        env.Add(key, new YamlValue(value));
        return this;
    }

    /// <summary>
    /// Adds a command to the service configuration in a Docker Compose file.
    /// </summary>
    /// <param name="value">The command to add to the service configuration.</param>
    /// <returns>The updated <see cref="ComposeService"/> instance.</returns>
    public ComposeService AddCommand(string value)
    {
        var commands = GetOrCreate<YamlArray>(DockerComposeYamlKeys.Command);
        commands.Add(new YamlValue(value));
        return this;
    }

    /// <summary>
    /// Sets the image property of the Docker Compose service.
    /// </summary>
    /// <param name="value">The name of the Docker image to set for the service.</param>
    /// <returns>The current instance of <see cref="ComposeService"/> to allow chaining of methods.</returns>
    public ComposeService WithImage(string value)
    {
        Replace(DockerComposeYamlKeys.Image, new YamlValue(value));
        return this;
    }
}
