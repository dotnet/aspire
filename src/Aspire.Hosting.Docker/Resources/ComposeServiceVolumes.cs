// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents the volumes configuration for a service in a Docker Compose file.
/// </summary>
/// <remarks>
/// This class is a specialized implementation of the <see cref="YamlArray"/> class, specifically used
/// to define and manage the volumes associated with a Docker Compose service. It enables the storage of
/// multiple volume definitions, where each volume may point to a host path, named volume, or other volume types
/// supported by Docker Compose.
/// </remarks>
public sealed class ComposeServiceVolumes : YamlArray
{
    /// <summary>
    /// Represents the volumes configuration for a service in a Docker Compose definition.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="YamlArray"/> and is used to manage the collection of volume definitions
    /// for a Docker Compose service. It supports defining multiple volumes that can reference host paths,
    /// named volumes, or other valid volume types as per Docker Compose specifications.
    /// </remarks>
    public ComposeServiceVolumes()
    {
    }
}
