// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the configuration for session affinity in a Kubernetes Service resource.
/// </summary>
/// <remarks>
/// This class is used to define session affinity configurations for Kubernetes services,
/// specifically to manage how traffic is directed to service endpoints based on session affinity settings.
/// </remarks>
[YamlSerializable]
public sealed class SessionAffinityConfigV1
{
    /// <summary>
    /// Gets or sets the client IP configuration used for session affinity.
    /// </summary>
    /// <remarks>
    /// This property specifies the configuration settings related to session affinity
    /// that are based on the client's IP address. It encapsulates parameters such as
    /// the timeout duration for session stickiness.
    /// </remarks>
    [YamlMember(Alias = "clientIP")]
    public ClientIPConfigV1 ClientIp { get; set; } = new();
}
