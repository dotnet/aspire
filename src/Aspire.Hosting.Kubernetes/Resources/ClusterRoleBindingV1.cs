// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a ClusterRoleBinding in a Kubernetes cluster.
/// </summary>
/// <remarks>
/// A ClusterRoleBinding grants access to cluster-scoped resources by binding a ClusterRole
/// to one or more subjects. It contains a reference to a ClusterRole and a collection
/// of subjects, such as users, groups, or service accounts, to which it applies.
/// </remarks>
[YamlSerializable]
public sealed class ClusterRoleBinding() : BaseKubernetesResource("rbac.authorization.k8s.io/v1", "ClusterRoleBinding")
{
    /// <summary>
    /// Gets or sets the RoleRef property, which contains information
    /// pointing to the Kubernetes role being referenced within the
    /// ClusterRoleBinding configuration. The RoleRef property defines
    /// the role name, kind, and the group for API access, to ensure the
    /// binding is correctly associated with the intended permissions and scope.
    /// </summary>
    [YamlMember(Alias = "roleRef")]
    public RoleRefV1 RoleRef { get; set; } = new();

    /// <summary>
    /// Gets the list of subjects associated with the ClusterRoleBinding.
    /// Subjects refer to the entities (users, groups, or service accounts) that the role binding applies to.
    /// </summary>
    /// <remarks>
    /// Each subject in the list can represent a specific user, group, or Kubernetes resource that is granted
    /// the permissions defined in the associated role. This property is initialized as an empty list and can
    /// contain zero or more subjects.
    /// </remarks>
    [YamlMember(Alias = "subjects")]
    public List<SubjectV1> Subjects { get; } = [];
}
