// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a collection of service ports for Docker Compose configurations.
/// Inherits from the <see cref="YamlArray"/> class to provide structure and functionality for handling an array of port definitions in YAML format.
/// </summary>
/// <remarks>
/// This class is used to define and manage port mappings within a Docker Compose service definition,
/// leveraging the features of <see cref="YamlArray"/> to work with individual port mappings as <see cref="YamlNode"/> instances.
/// </remarks>
public sealed class ComposeServicePorts : YamlArray
{
    /// <summary>
    /// Represents a collection of service ports within a Docker Compose configuration.
    /// Extends the <see cref="YamlArray"/> class to utilize YAML array functionalities for managing port definitions.
    /// </summary>
    public ComposeServicePorts()
    {
    }
}
