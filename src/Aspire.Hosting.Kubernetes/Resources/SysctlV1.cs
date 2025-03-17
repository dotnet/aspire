// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a sysctl setting in the Pod's security context in a Kubernetes environment.
/// </summary>
/// <remarks>
/// A sysctl is a kernel parameter that can be modified at runtime. The <see cref="SysctlV1"/> class
/// is used to define specific sysctl settings as part of the Pod's security context configuration.
/// These settings allow more granular control over kernel-level behaviors for Pods.
/// </remarks>
[YamlSerializable]
public sealed class SysctlV1
{
    /// <summary>
    /// Gets or sets the name of the sysctl parameter.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value of the sysctl parameter.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string Value { get; set; } = null!;
}
