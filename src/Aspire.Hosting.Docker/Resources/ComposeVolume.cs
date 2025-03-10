// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a Docker Compose volume definition within a YAML configuration.
/// </summary>
/// <remarks>
/// The <c>ComposeVolume</c> class is a representation of a volume element in a Docker Compose file.
/// It extends the <c>YamlObject</c> class, allowing manipulation and serialization of YAML structures.
/// This class facilitates adding configuration options to a volume, such as specifying whether the volume is external.
/// </remarks>
public sealed class ComposeVolume : YamlObject
{
    /// <summary>
    /// Represents a Docker Compose volume configuration within a YAML structure.
    /// </summary>
    /// <remarks>
    /// This class is used to define and configure Docker volumes in a YAML-based representation,
    /// such as a Docker Compose file. Volumes in Docker are used to persist data outside the lifecycle
    /// of a container, sharing data between containers or providing storage on the host machine.
    /// </remarks>
    public ComposeVolume() { }

    /// <summary>
    /// Configures the volume as external or not in the YAML configuration.
    /// </summary>
    /// <param name="external">A boolean value indicating whether the volume should be marked as external.</param>
    /// <returns>Returns the instance of <see cref="ComposeVolume"/> with the updated configuration.</returns>
    public ComposeVolume SetExternal(bool external)
    {
        Add("external", new YamlValue(external));
        return this;
    }
}
