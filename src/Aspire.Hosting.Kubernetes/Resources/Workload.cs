// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// The base class for Kubernetes workload resources, such as StatefulSets and Deployments.
/// </summary>
public abstract class Workload(string apiVersion, string kind) : BaseKubernetesResource(apiVersion, kind)
{
    /// <summary>
    /// Gets the pod template for the workload.
    /// </summary>
    public abstract PodTemplateSpecV1 GetPodTemplate();
}
