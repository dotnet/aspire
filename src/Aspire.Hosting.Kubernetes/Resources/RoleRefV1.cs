// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a reference to a Kubernetes Role or ClusterRole.
/// </summary>
/// <remarks>
/// The RoleRefV1 class is used to specify which role or cluster role a RoleBinding or ClusterRoleBinding references.
/// It includes details about the kind of role, the name of the role, and the API group to which the role belongs.
/// </remarks>
[YamlSerializable]
public sealed class RoleRefV1
{
    /// <summary>
    /// Gets or sets the kind of the referenced role resource.
    /// </summary>
    /// <remarks>
    /// The Kind property is used to specify the type of resource being pointed to.
    /// This includes values like "Role" or "ClusterRole" in Kubernetes.
    /// </remarks>
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = null!;

    /// Gets or sets the name of the referenced role. This property specifies the name of the role or cluster role that this RoleRefV1 object refers to.
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API group associated with the Kubernetes resource.
    /// The API group is used to specify the group of the resource, allowing the use of resources across different API versions.
    /// </summary>
    [YamlMember(Alias = "apiGroup")]
    public string ApiGroup { get; set; } = null!;
}
