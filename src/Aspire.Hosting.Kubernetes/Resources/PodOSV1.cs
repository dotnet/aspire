// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the operating system information for a Pod in a Kubernetes cluster.
/// </summary>
[YamlSerializable]
public sealed class PodOsv1
{
    /// <summary>
    /// Gets or sets the name of the Pod operating system.
    /// This property represents the operating system name as defined in the pod configuration.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;
}
