// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

/// <summary>
/// Represents a collection of additional labels that can be associated with a Docker service.
/// This class is used to define extra metadata in the form of key-value pairs.
/// </summary>
[YamlSerializable]
public sealed class LabelSpecs
{
    /// <summary>
    /// Gets or sets the collection of additional labels as a dictionary of key-value pairs.
    /// These labels can be used to provide metadata or additional configuration
    /// for a service node in a Swarm environment.
    /// </summary>
    [YamlMember(Alias = "additional_labels")]
    public Dictionary<string, string> AdditionalLabels { get; set; } = [];
}
