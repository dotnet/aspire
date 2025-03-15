// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a PersistentVolume resource in Kubernetes.
/// </summary>
/// <remarks>
/// PersistentVolume is a cluster-level storage resource within Kubernetes. It defines a
/// piece of storage that has been provisioned by an administrator or dynamically
/// provisioned using StorageClasses. This resource is independent of individual Pods
/// and remains available beyond the lifecycle of any Pod utilizing it.
/// PersistentVolume is designed to manage storage that is not tied to a single Pod or
/// namespace, enabling data persistence across Pod restarts or failures. It includes
/// properties to define storage class, access modes, and volume attributes.
/// </remarks>
[YamlSerializable]
public sealed class PersistentVolume() : BaseKubernetesResource("v1", "PersistentVolume")
{
    /// <summary>
    /// Gets or sets the specification for the Kubernetes PersistentVolume resource.
    /// This property defines the detailed configuration of the PersistentVolume,
    /// including storage capacity, access modes, volume mode, node affinity,
    /// and reclaim policy. The specification provides granular control over the
    /// behavior and capabilities of the PersistentVolume.
    /// </summary>
    [YamlMember(Alias = "spec")]
    public PersistentVolumeSpecV1 Spec { get; set; } = new();
}
