// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ComposeNodes;

/// <summary>
/// Represents a configuration object in a Docker Compose file.
/// </summary>
/// <remarks>
/// This class models a configuration entry within a Docker Compose file, such as
/// file-based or external configurations. It includes properties to define the
/// source file, external flag, custom name, and additional labels for the configuration.
/// </remarks>
[YamlSerializable]
public sealed class Config
{
    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// This property is used to specify the file containing
    /// the configuration data for the service or component.
    /// </summary>
    [YamlMember(Alias = "file")]
    public string? File { get; set; }

    /// <summary>
    /// Indicates whether the configuration is external to the current project context.
    /// When set to true, the configuration will not be managed or created by the Compose file;
    /// instead, it references an existing resource outside the current scope.
    /// If null, the external status is not explicitly specified.
    /// </summary>
    [YamlMember(Alias = "external")]
    public bool? External { get; set; }

    /// <summary>
    /// Represents the name of the Docker configuration resource as defined in the Compose file.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Represents a collection of key-value pairs used as metadata
    /// for configuration objects. The labels provide additional
    /// descriptive information, which can be utilized for tagging,
    /// grouping, or identification purposes.
    /// </summary>
    [YamlMember(Alias = "labels", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, string> Labels { get; set; } = [];
}

