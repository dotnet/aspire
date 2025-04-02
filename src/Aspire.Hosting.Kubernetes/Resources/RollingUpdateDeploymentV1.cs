// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the rolling update configuration for a Kubernetes Deployment.
/// Defines the parameters for controlling the behavior of updating Pods in a Deployment.
/// </summary>
[YamlSerializable]
public sealed class RollingUpdateDeploymentV1
{
    /// <summary>
    /// Gets or sets the maximum number of additional pods that can be scheduled above the desired number of pods during a rolling update in a deployment.
    /// </summary>
    [YamlMember(Alias = "maxSurge")]
    public int MaxSurge { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of pods that can be unavailable during the rolling update process.
    /// This property controls how workloads are updated without causing application downtime.
    /// </summary>
    [YamlMember(Alias = "maxUnavailable")]
    public int MaxUnavailable { get; set; }
}
