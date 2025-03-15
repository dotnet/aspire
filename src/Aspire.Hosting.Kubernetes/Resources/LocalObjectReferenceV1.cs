// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a reference to a Kubernetes object by name.
/// </summary>
/// <remarks>
/// This class is used to refer to a specific Kubernetes object within the same namespace
/// by specifying its name. It is commonly referenced in other resources to associate
/// with configurations or secrets.
/// </remarks>
[YamlSerializable]
public sealed class LocalObjectReferenceV1
{
    /// <summary>
    /// Gets or sets the name of the referenced object within the same namespace.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;
}
