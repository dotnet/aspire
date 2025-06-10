// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the specification for a Kubernetes Namespace resource in the v1 API version.
/// </summary>
/// <remarks>
/// This class provides configuration details for a Kubernetes Namespace, specifically the
/// associated finalizers. Finalizers are used to ensure proper cleanup or processing of
/// resources before deletion. The NamespaceSpecV1 object is referenced within a Kubernetes
/// Namespace instance.
/// </remarks>
[YamlSerializable]
public sealed class NamespaceSpecV1
{
    /// <summary>
    /// Gets the list of finalizers associated with the namespace.
    /// Finalizers are used to define actions or hooks that must be completed before a namespace is deleted.
    /// This ensures specific cleanup tasks are completed properly before the resource is removed.
    /// </summary>
    [YamlMember(Alias = "finalizers")]
    public List<string> Finalizers { get; } = [];
}
