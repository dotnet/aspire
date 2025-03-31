// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ComposeNodes;

/// <summary>
/// Represents a generic named member within a Docker Compose configuration structure.
/// </summary>
/// <remarks>
/// This abstract class serves as a base for defining named components, such as networks, services, and others,
/// within Docker Compose files. The Name property must be explicitly defined for each derived class implementation.
/// </remarks>
public abstract class NamedComposeMember
{
    /// <summary>
    /// Gets or sets the name of the Docker Compose member.
    /// </summary>
    /// <remarks>
    /// This property is used to uniquely identify the member (e.g., network, service) within
    /// the Docker Compose configuration. It must be explicitly defined for all derived types.
    /// </remarks>
    [YamlIgnore]
    public required string Name { get; set; }
}
