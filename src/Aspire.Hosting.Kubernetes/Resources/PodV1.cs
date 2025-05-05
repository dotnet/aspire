// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes Pod resource in the v1 API version.
/// </summary>
/// <remarks>
/// A Pod is the smallest and simplest Kubernetes resource that serves as a unit of deployment in the cluster.
/// It encapsulates an application container or multiple containers, along with storage resources, networking,
/// and configuration options to manage the execution of the containerized application. Pods are the foundational
/// building blocks of Kubernetes workloads and are managed by controllers like Deployments, ReplicaSets, and Jobs.
/// </remarks>
[YamlSerializable]
public sealed class Pod() : BaseKubernetesResource("v1", "Pod")
{
    /// <summary>
    /// Represents the specification for the Kubernetes Pod resource.
    /// Contains detailed configuration for the behavior and lifecycle management
    /// of the Kubernetes Pod. This property is of type <see cref="PodSpecV1"/>.
    /// </summary>
    [YamlMember(Alias = "spec")]
    public PodSpecV1 Spec { get; set; } = new();
}
