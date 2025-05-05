// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an allowed host path for a PodSecurityPolicy in Kubernetes.
/// </summary>
/// <remarks>
/// This class defines constraints that determine which host paths are allowed to be used by containers
/// backed by PodSecurityPolicies. It enables setting a specific path prefix and/or defining if the path
/// is read-only.
/// </remarks>
[YamlSerializable]
public sealed class AllowedHostPathV1Beta1
{
    /// <summary>
    /// Gets or sets the path prefix for the allowed host path.
    /// This property specifies the path prefix that will be matched
    /// against a volume's path. Only paths with this prefix will be allowed.
    /// </summary>
    [YamlMember(Alias = "pathPrefix")]
    public string PathPrefix { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the host path is read-only.
    /// When set to true, the associated path can only be accessed in read-only mode.
    /// </summary>
    [YamlMember(Alias = "readOnly")]
    public bool? ReadOnly { get; set; }
}
