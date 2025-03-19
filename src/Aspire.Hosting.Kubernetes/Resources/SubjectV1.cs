// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a subject in Kubernetes RoleBinding or ClusterRoleBinding resources.
/// </summary>
/// <remarks>
/// A subject identifies a Kubernetes entity, such as a user, group, or service account,
/// that is granted permissions through a RoleBinding or ClusterRoleBinding.
/// </remarks>
[YamlSerializable]
public sealed class SubjectV1
{
    /// <summary>
    /// Gets or sets the kind of the subject.
    /// This property specifies the type of the Kubernetes subject, such as "User", "Group", or "ServiceAccount".
    /// </summary>
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the Kubernetes resource subject.
    /// Represents the specific subject entity associated with the resource.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the namespace associated with the subject.
    /// This property is used to specify the namespace in which the subject resides.
    /// It is typically applicable in contexts where namespace-scoped resources are involved.
    /// </summary>
    [YamlMember(Alias = "namespace")]
    public string Namespace { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API group of the subject in Kubernetes.
    /// This property specifies the group of the referenced resource,
    /// which is used to differentiate resources with the same kind or name belonging to different API groups.
    /// </summary>
    [YamlMember(Alias = "apiGroup")]
    public string ApiGroup { get; set; } = null!;
}
