// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a PodReadinessGate configuration in a Kubernetes Pod specification.
/// A readiness gate defines additional readiness conditions that a pod needs to satisfy
/// to be considered ready. This is typically used for custom or external conditions.
/// </summary>
[YamlSerializable]
public sealed class PodReadinessGateV1
{
    /// <summary>
    /// Gets or sets the condition type associated with the PodReadinessGateV1.
    /// Represents the specific type of condition that must be satisfied for the Pod to be declared ready.
    /// </summary>
    [YamlMember(Alias = "conditionType")]
    public string ConditionType { get; set; } = null!;
}
