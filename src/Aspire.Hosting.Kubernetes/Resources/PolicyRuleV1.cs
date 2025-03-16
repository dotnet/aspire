// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes PolicyRule resource in API version v1.
/// </summary>
/// <remarks>
/// A PolicyRule defines a set of permissions within the Role-Based Access Control (RBAC) system.
/// It specifies allowed actions on the Kubernetes API and can target specific resources, resource names,
/// or non-resource URLs. The rule is composed of different lists that determine the API groups,
/// resources, resource names, verbs, and non-resource URLs it applies to.
/// </remarks>
[YamlSerializable]
public sealed class PolicyRuleV1
{
    /// <summary>
    /// Gets the list of API groups that the policy rule applies to.
    /// Each entry in the list specifies the name of an API group to which the rule grants access.
    /// An empty list or null indicates that the rule applies to all API groups within the scope of the rule.
    /// </summary>
    [YamlMember(Alias = "apiGroups")]
    public List<string> ApiGroups { get; } = [];

    /// <summary>
    /// Gets the list of URLs that do not correspond to standard Kubernetes resources.
    /// These URLs are typically used to define permissions or access control for
    /// specific non-resource requests within the cluster, such as custom API paths
    /// or administrative endpoints.
    /// </summary>
    [YamlMember(Alias = "nonResourceURLs")]
    public List<string> NonResourceUrLs { get; } = [];

    /// <summary>
    /// Gets the list of resource names that the policy rule applies to.
    /// Resource names are specific objects within a resource type, such as a specific ConfigMap or Pod.
    /// This property allows for fine-grained control of access to named resources.
    /// </summary>
    [YamlMember(Alias = "resourceNames")]
    public List<string> ResourceNames { get; } = [];

    /// <summary>
    /// Gets the list of resource names that the policy applies to in a Kubernetes cluster.
    /// These resources generally refer to resource types such as pods, services, deployments, etc.,
    /// and must align with the resource types defined in the Kubernetes API.
    /// </summary>
    [YamlMember(Alias = "resources")]
    public List<string> Resources { get; } = [];

    /// <summary>
    /// Gets the list of actions or operations that are allowed or applicable for this policy rule.
    /// This property defines the specific set of verbs such as "get", "list", "watch", "create", "delete", etc.,
    /// that the policy rule applies to within the specified resources or URLs.
    /// </summary>
    [YamlMember(Alias = "verbs")]
    public List<string> Verbs { get; } = [];
}
