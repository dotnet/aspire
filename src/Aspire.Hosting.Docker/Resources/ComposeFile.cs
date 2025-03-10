// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a Docker Compose file as a YAML object, which encapsulates services, networks,
/// volumes, and profiles defined within a Docker Compose configuration.
/// </summary>
/// <remarks>
/// This class extends the functionality of <see cref="YamlObject"/> to provide specific
/// operations for managing Docker Compose files.
/// </remarks>
public sealed class ComposeFile : YamlObject
{
    /// <summary>
    /// Represents a Docker Compose file as a YAML object and provides utility methods
    /// for managing top-level sections such as services, networks, volumes, and profiles.
    /// </summary>
    public ComposeFile(string? existingNetworkName = null)
    {
        InitializeComposeNetwork(existingNetworkName);
    }

    /// <summary>
    /// Creates a <see cref="ComposeFile"/> instance by deserializing the provided YAML-formatted string
    /// and extracting the known sections such as services, networks, volumes, and profiles.
    /// </summary>
    /// <param name="yaml">The YAML-formatted string to be deserialized into a <see cref="ComposeFile"/>.</param>
    /// <returns>A new <see cref="ComposeFile"/> instance representing the deserialized YAML structure with the known sections replaced if present.</returns>
    public static new ComposeFile FromYaml(string yaml)
    {
        var obj = YamlObject.FromYaml(yaml);
        var compose = new ComposeFile();

        // Replace known sections if present
        if (obj.Get(DockerComposeYamlKeys.Services) is { } svcs)
        {
            compose.Replace(DockerComposeYamlKeys.Services, svcs);
        }

        if (obj.Get(DockerComposeYamlKeys.Networks) is { } nets)
        {
            compose.Replace(DockerComposeYamlKeys.Networks, nets);
        }

        if (obj.Get(DockerComposeYamlKeys.Volumes) is { } vols)
        {
            compose.Replace(DockerComposeYamlKeys.Volumes, vols);
        }

        if (obj.Get(DockerComposeYamlKeys.Profiles) is { } profs)
        {
            compose.Replace(DockerComposeYamlKeys.Profiles, profs);
        }

        return compose;
    }

    /// <summary>
    /// Adds a service to the Docker Compose file under the services section.
    /// </summary>
    /// <param name="serviceName">The name of the service to be added.</param>
    /// <param name="service">The <see cref="ComposeService"/> instance representing the service configuration.</param>
    /// <returns>The current instance of <see cref="ComposeFile"/> for method chaining.</returns>
    public ComposeFile AddService(string serviceName, ComposeService service)
    {
        var services = GetOrCreate<YamlObject>(DockerComposeYamlKeys.Services);
        services.Add(serviceName, service);
        return this;
    }

    /// Adds a network definition to the ComposeFile instance.
    /// <param name="networkName">The name of the network to be added. This serves as the key for the network definition.</param>
    /// <param name="network">The network definition as a YamlObject, specifying the properties of the network.</param>
    /// <returns>Returns the updated ComposeFile instance with the new network added.</returns>
    public ComposeFile AddNetwork(string networkName, ComposeNetwork network)
    {
        var networks = GetOrCreate<YamlObject>(DockerComposeYamlKeys.Networks);
        networks.Add(networkName, network);
        return this;
    }

    /// <summary>
    /// Adds a specified volume to the 'volumes' section of the Compose file.
    /// </summary>
    /// <param name="volumeName">The name of the volume to add.</param>
    /// <param name="volume">The volume configuration as a <see cref="YamlObject"/>.</param>
    /// <returns>The updated <see cref="ComposeFile"/> instance.</returns>
    public ComposeFile AddVolume(string volumeName, ComposeVolume volume)
    {
        var volumes = GetOrCreate<YamlObject>(DockerComposeYamlKeys.Volumes);
        volumes.Add(volumeName, volume);
        return this;
    }

    private void InitializeComposeNetwork(string? existingNetworkName = null)
    {
        var networkName = existingNetworkName ?? "aspire";

        var network = new ComposeNetwork();
        network.SetDriver("bridge");

        if (!string.IsNullOrEmpty(existingNetworkName))
        {
            network.SetExternal();
        }

        AddNetwork(networkName, network);
    }
}
