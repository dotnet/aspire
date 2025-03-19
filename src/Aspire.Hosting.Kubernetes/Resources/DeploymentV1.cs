// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes Deployment resource for managing application deployments in a cluster.
/// </summary>
/// <remarks>
/// The Deployment class is a sealed class derived from the BaseKubernetesResource. It defines the
/// desired state and behavior of a deployment within a Kubernetes cluster, including specifications
/// such as the number of replicas, update strategy, and pod templates.
/// It uses the "apps/v1" API version and the resource kind "Deployment".
/// </remarks>
[YamlSerializable]
public sealed class Deployment() : BaseKubernetesResource("apps/v1", "Deployment")
{
    /// <summary>
    /// Gets or sets the specification of the Kubernetes Deployment resource.
    /// </summary>
    /// <remarks>
    /// This property defines the detailed configuration and desired state of the Deployment resource.
    /// It includes settings such as the desired number of replicas, update strategies, pod templates,
    /// label selectors, and other deployment-related configurations.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public DeploymentSpecV1 Spec { get; set; } = new();
}
