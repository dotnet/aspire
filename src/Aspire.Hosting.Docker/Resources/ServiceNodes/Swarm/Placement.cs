// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Represents the placement configuration for a Docker service in a Swarm cluster.
/// This class is used to define specific constraints and preferences for the placement of tasks or containers.
/// </summary>
[YamlSerializable]
public sealed class Placement
{
    /// <summary>
    /// A collection of constraints that define where tasks can be scheduled
    /// within a Swarm cluster. These constraints act as filters, ensuring that
    /// tasks are only placed on nodes that match all the specified conditions.
    /// </summary>
    [YamlMember(Alias = "constraints")]
    public List<string>? Constraints { get; set; }

    /// <summary>
    /// Gets or sets the preferences for the placement strategy in the Swarm service nodes.
    /// </summary>
    /// <remarks>
    /// The preferences configuration is represented as a list of dictionaries, where each dictionary
    /// specifies key-value pairs that define custom placement preferences for the service.
    /// </remarks>
    [YamlMember(Alias = "preferences")]
    public List<Dictionary<string, string>>? Preferences { get; set; }
}
