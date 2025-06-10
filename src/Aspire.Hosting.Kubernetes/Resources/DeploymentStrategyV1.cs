// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Describes the strategy used to replace old Pods with new ones in a Kubernetes Deployment.
/// </summary>
[YamlSerializable]
public sealed class DeploymentStrategyV1
{
    /// <summary>
    /// Represents the configuration for rolling update behavior in a deployment strategy.
    /// Defines parameters such as the maximum number of pods that can be unavailable during
    /// the update process or the maximum number of extra pods that can be created
    /// during a rolling update.
    /// </summary>
    [YamlMember(Alias = "rollingUpdate")]
    public RollingUpdateDeploymentV1 RollingUpdate { get; set; } = new();

    /// <summary>
    /// Specifies the type of deployment strategy to be used. Common values include "Recreate" or "RollingUpdate".
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "RollingUpdate";
}
