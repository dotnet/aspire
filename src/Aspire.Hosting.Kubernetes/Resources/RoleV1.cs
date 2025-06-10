// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Defines a Kubernetes Role resource within the "rbac.authorization.k8s.io/v1" API group.
/// </summary>
/// <remarks>
/// A Role is used to grant access to resources within a specific namespace in Kubernetes.
/// It is composed of a collection of policy rules that determine the permitted actions
/// (e.g., get, list, create) on specified resources within the namespace.
/// This class extends the BaseKubernetesResource for consistent handling of Kubernetes resources.
/// </remarks>
/// <seealso cref="PolicyRuleV1"/>
/// <seealso cref="BaseKubernetesResource"/>
[YamlSerializable]
public sealed class Role() : BaseKubernetesResource("rbac.authorization.k8s.io/v1", "Role")
{
    /// <summary>
    /// Represents the list of policy rules associated with a Kubernetes Role resource.
    /// </summary>
    /// <remarks>
    /// The Rules property defines the set of permissions assigned to the Role in the form of policy rules.
    /// Each rule specifies which actions are permitted or denied for particular resources, resource names,
    /// API groups, verbs, or non-resource URLs. The property is a collection of <see cref="PolicyRuleV1" />
    /// objects and dictates the Role's scope and access within the Kubernetes environment.
    /// </remarks>
    [YamlMember(Alias = "rules")]
    public List<PolicyRuleV1> Rules { get; } = [];
}
