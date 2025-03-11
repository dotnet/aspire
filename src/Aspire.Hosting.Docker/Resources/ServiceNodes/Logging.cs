// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes;

/// <summary>
/// Represents the logging configuration for a service in a containerized environment.
/// </summary>
/// <remarks>
/// This class defines the logging driver and its associated options that can be used
/// to control how logs are handled for a service. It is typically used within the context
/// of container orchestration platforms to configure logging behavior at a service level.
/// </remarks>
[YamlSerializable]
public sealed class Logging
{
    /// <summary>
    /// Gets or sets the logging driver to be used for the service node.
    /// This property specifies the logging mechanism, such as "json-file", "syslog", or "none",
    /// to determine how log data is managed.
    /// </summary>
    [YamlMember(Alias = "driver")]
    public string? Driver { get; set; }

    /// <summary>
    /// Gets or sets a collection of key-value pairs representing the logging driver options.
    /// These options are configuration parameters used to customize the behavior of the logging driver.
    /// </summary>
    [YamlMember(Alias = "options", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, string> Options { get; set; } = [];
}
