// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a network configuration for a Docker Compose file.
/// </summary>
/// <remarks>
/// This class provides functionality to define and configure a network in a Docker Compose
/// YAML file. It inherits from the <see cref="YamlObject"/> base class and uses key-value
/// pairs to set properties for the network configuration.
/// </remarks>
public sealed class ComposeNetwork : YamlObject
{
    /// <summary>
    /// Sets the driver property for the current ComposeNetwork instance.
    /// </summary>
    /// <param name="driver">The name of the network driver to set.</param>
    /// <returns>The current <see cref="ComposeNetwork"/> instance with the updated driver property.</returns>
    public ComposeNetwork SetDriver(string driver)
    {
        Add(DockerComposeYamlKeys.Driver, new YamlValue(driver));
        return this;
    }

    /// <summary>
    /// Sets the external property for the current ComposeNetwork instance.
    /// </summary>
    /// <returns>The current <see cref="ComposeNetwork"/> instance with the external property set to true.</returns>
    public ComposeNetwork SetExternal()
    {
        Add(DockerComposeYamlKeys.External, new YamlValue(true));
        return this;
    }
}
