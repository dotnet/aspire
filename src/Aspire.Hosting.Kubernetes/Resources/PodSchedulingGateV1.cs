// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a scheduling gate for a pod, used in conjunction with Kubernetes scheduling gates mechanism.
/// </summary>
/// <remarks>
/// A scheduling gate is an abstraction that allows postponing the scheduling of a pod until certain conditions
/// are met. This is typically utilized in complex scheduling scenarios.
/// </remarks>
[YamlSerializable]
public sealed class PodSchedulingGateV1
{
    /// <summary>
    /// Gets or sets the name of the scheduling gate.
    /// This is used to identify the specific scheduling gate for the pod.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;
}
