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
    /// Represents a Docker Compose network resource, providing a way to configure network-specific settings.
    /// </summary>
    /// <remarks>
    /// The <see cref="ComposeNetwork"/> class enables the creation and configuration of a Docker Compose network
    /// using key-value pairs for properties such as the network driver. This class extends the functionality of
    /// the <see cref="YamlObject"/>, allowing seamless integration and manipulation of YAML configurations that
    /// represent a Docker Compose network.
    /// </remarks>
    public ComposeNetwork() { }

    /// <summary>
    /// Sets the driver property for the current ComposeNetwork instance.
    /// </summary>
    /// <param name="driver">The name of the network driver to set.</param>
    /// <returns>The current <see cref="ComposeNetwork"/> instance with the updated driver property.</returns>
    public ComposeNetwork SetDriver(string driver)
    {
        Add("driver", new YamlValue(driver));
        return this;
    }
}
