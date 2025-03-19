// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes RoleBinding resource, binding a Role or ClusterRole to a set of subjects (users, groups, or service accounts).
/// </summary>
/// <remarks>
/// A RoleBinding grants the permissions defined in a Role to specific users, groups, or service accounts within a namespace.
/// It supports the inclusion of multiple subjects and references a single role through the RoleRef property.
/// The RoleBinding resource is namespace-scoped and helps manage access control within the Kubernetes RBAC framework.
/// </remarks>
[YamlSerializable]
public sealed class RoleBinding() : BaseKubernetesResource("rbac.authorization.k8s.io/v1", "RoleBinding")
{
    /// <summary>
    /// Gets or sets the reference to the role or cluster role that the binding applies to.
    /// </summary>
    /// <remarks>
    /// The RoleRef property specifies the target role or cluster role the RoleBinding object binds to.
    /// It contains information about the API group, the kind of the role (Role or ClusterRole),
    /// and the name of the role.
    /// </remarks>
    [YamlMember(Alias = "roleRef")]
    public RoleRefV1 RoleRef { get; set; } = new();

    /// <summary>
    /// Represents a collection of Subjects that define the identities (users, groups, or service accounts)
    /// bound to a specific Role or ClusterRole in Kubernetes.
    /// </summary>
    /// <remarks>
    /// Each item in the Subjects list specifies an entity (such as a user, group, or service account) that
    /// is granted permissions associated with the referenced Role or ClusterRole. This property is a key
    /// component of a RoleBinding or ClusterRoleBinding resource.
    /// </remarks>
    [YamlMember(Alias = "subjects")]
    public List<SubjectV1> Subjects { get; } = [];
}
