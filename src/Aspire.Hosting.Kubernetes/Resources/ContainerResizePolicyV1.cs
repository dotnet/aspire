// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a policy for resizing containers in a Kubernetes environment.
/// </summary>
/// <remarks>
/// This class defines the configuration for a container's resize policy, specifically
/// the associated resource and the restart behavior.
/// </remarks>
[YamlSerializable]
public sealed class ContainerResizePolicyV1
{
    /// <summary>
    /// Gets or sets the name of the resource associated with the container's resize policy.
    /// </summary>
    [YamlMember(Alias = "resourceName")]
    public string ResourceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the restart policy for the container. Determines the behavior
    /// of the container regarding restarts upon failure or completion.
    /// Typical options could include policies like "Always", "OnFailure", or "Never".
    /// </summary>
    [YamlMember(Alias = "restartPolicy")]
    public string RestartPolicy { get; set; } = null!;
}
