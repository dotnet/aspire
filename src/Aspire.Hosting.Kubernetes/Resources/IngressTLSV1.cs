// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the TLS configuration for an Ingress resource in Kubernetes (networking.k8s.io/v1).
/// </summary>
/// <remarks>
/// This class defines the Transport Layer Security (TLS) settings for securing Ingress traffic
/// to one or more hosts. It specifies the details such as the associated secret that contains
/// the TLS certificate and key, and the list of hosts to which the TLS settings apply.
/// </remarks>
[YamlSerializable]
public sealed class IngressTLSV1
{
    /// <summary>
    /// Represents the name of the Kubernetes Secret containing the TLS certificate and key.
    /// This property is used to secure communication for the specified hosts in an ingress resource.
    /// </summary>
    [YamlMember(Alias = "secretName")]
    public string SecretName { get; set; } = null!;

    /// <summary>
    /// Gets a list of hostnames associated with the ingress TLS configuration.
    /// </summary>
    /// <remarks>
    /// This property represents a collection of hosts that are specified for
    /// the ingress TLS configuration. These hosts are used for secure transport
    /// and typically represent domain names that are covered by the associated
    /// TLS secret. The list may contain multiple entries depending on the
    /// assigned hosts.
    /// </remarks>
    [YamlMember(Alias = "hosts")]
    public List<string> Hosts { get; } = [];
}
