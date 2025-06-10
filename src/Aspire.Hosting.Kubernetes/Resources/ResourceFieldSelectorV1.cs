// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// ResourceFieldSelectorV1 provides a means to reference a resource field's value, such as CPU or memory,
/// from a container in a Kubernetes resource. It allows for detailed selection of the required resource
/// and includes optional configuration to specify a container and/or a divisor for resource scaling.
/// </summary>
[YamlSerializable]
public sealed class ResourceFieldSelectorV1
{
    /// <summary>
    /// Specifies the name of the container from which to select resource data.
    /// This property identifies the container within a Kubernetes environment
    /// where the resource usage will be retrieved.
    /// </summary>
    [YamlMember(Alias = "containerName")]
    public string ContainerName { get; set; } = null!;

    /// <summary>
    /// Specifies the resource to select from a particular container in a Kubernetes environment.
    /// The resource field is used to identify the particular resource attribute (e.g., CPU, memory)
    /// of a container to be referenced within the context of resource management or metrics.
    /// </summary>
    [YamlMember(Alias = "resource")]
    public string Resource { get; set; } = null!;

    /// <summary>
    /// Gets or sets the quantity used as the divisor in the resource field selector.
    /// This property defines the scaling factor applied to the resource quantity
    /// specified in the selector.
    /// </summary>
    [YamlMember(Alias = "divisor")]
    public string Divisor { get; set; } = null!;
}
