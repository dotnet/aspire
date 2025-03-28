// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes;

/// <summary>
/// Represents the IP Address Management (IPAM) configuration for a Docker network.
/// </summary>
/// <remarks>
/// This class defines the properties related to IPAM, which is responsible
/// for assigning, managing, and configuring IP-related settings for Docker networks.
/// The configuration includes the driver used for IPAM, specific configuration details,
/// and additional options for customization.
/// </remarks>
[YamlSerializable]
public sealed class Ipam
{
    /// <summary>
    /// Gets or sets the driver used by the IPAM (IP Address Management) configuration.
    /// The driver specifies the type of IPAM driver to be used in the Docker network configuration.
    /// </summary>
    [YamlMember(Alias = "driver")]
    public string? Driver { get; set; }

    /// <summary>
    /// Represents a configuration for IP Address Management (IPAM).
    /// This property is a collection of key-value pairs that define
    /// specific IPAM configuration settings.
    /// </summary>
    [YamlMember(Alias = "config", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public List<Dictionary<string, string>> Config { get; set; } = [];

    /// <summary>
    /// A collection of key-value pairs representing options for the IPAM configuration.
    /// </summary>
    /// <remarks>
    /// The Options property allows for specifying additional configuration parameters
    /// for the IPAM (IP Address Management) system. It is represented as a dictionary
    /// where each key corresponds to a particular option name and the value corresponds
    /// to its respective setting.
    /// </remarks>
    [YamlMember(Alias = "options", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, string> Options { get; set; } = [];
}
