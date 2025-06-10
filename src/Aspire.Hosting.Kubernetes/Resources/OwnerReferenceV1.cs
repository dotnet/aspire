// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a reference to the owner of a Kubernetes resource. This reference is often used to establish relationships
/// between dependent resources and ensure cascading deletion when the owner resource is removed.
/// </summary>
[YamlSerializable]
public sealed class OwnerReferenceV1 : BaseKubernetesObject
{
    /// <summary>
    /// Gets or sets the unique identifier (UID) of the owner. This value is used to uniquely
    /// distinguish the owner resource within a Kubernetes cluster.
    /// </summary>
    [YamlMember(Alias = "uid")]
    public string Uid { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the Kubernetes resource associated with the owner reference.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Specifies whether the deletion of the owner object should be blocked
    /// if this dependent object still exists. If set to true, the owner object
    /// cannot be deleted until this dependent object is removed.
    /// This property is typically used in Kubernetes garbage collection mechanisms.
    /// </summary>
    [YamlMember(Alias = "blockOwnerDeletion")]
    public bool? BlockOwnerDeletion { get; set; }

    /// <summary>
    /// A property that indicates whether the current object is a controller for the associated resource.
    /// If true, it signifies that this object actively manages the resource and ensures its state matches
    /// the desired state defined by the controller.
    /// </summary>
    [YamlMember(Alias = "controller")]
    public bool? Controller { get; set; }
}
