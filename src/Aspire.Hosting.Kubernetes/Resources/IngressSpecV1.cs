// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the specification of an Ingress resource in Kubernetes (networking.k8s.io/v1).
/// </summary>
/// <remarks>
/// This class defines the configuration options for an Ingress resource to manage HTTP and HTTPS
/// access to services in a Kubernetes cluster. It includes the ability to set default backends,
/// define rules, specify TLS configurations, and optionally set an ingress class name.
/// </remarks>
[YamlSerializable]
public sealed class IngressSpecV1
{
    /// <summary>
    /// Defines the default backend for a Kubernetes ingress resource.
    /// </summary>
    /// <remarks>
    /// The default backend is used to handle requests that do not match any of the defined rules
    /// in the ingress configuration. It can specify a service or other resource to respond to unmatched traffic.
    /// </remarks>
    [YamlMember(Alias = "defaultBackend")]
    public IngressBackendV1 DefaultBackend { get; set; } = null!;

    /// <summary>
    /// Specifies the IngressClass associated with the Kubernetes Ingress resource.
    /// </summary>
    /// <remarks>
    /// The IngressClassName defines which controller will handle and process the associated ingress rules.
    /// If this property is not set, the Ingress resource will not be associated with a specific controller.
    /// </remarks>
    [YamlMember(Alias = "ingressClassName")]
    public string IngressClassName { get; set; } = null!;

    /// <summary>
    /// Gets the collection of ingress rules associated with the Kubernetes ingress resource.
    /// </summary>
    /// <remarks>
    /// The <c>Rules</c> property defines the routing configuration for incoming network traffic,
    /// specified as a list of <see cref="IngressRuleV1"/> objects. Each rule corresponds to a set
    /// of conditions under which the traffic is routed to a specific backend.
    /// </remarks>
    [YamlMember(Alias = "rules")]
    public List<IngressRuleV1> Rules { get; } = [];

    /// <summary>
    /// Represents the TLS configuration for a Kubernetes ingress resource.
    /// </summary>
    /// <remarks>
    /// This property defines a collection of TLS settings used to secure ingress traffic. Each
    /// entry in the collection specifies a TLS certificate and the associated set of hosts.
    /// </remarks>
    [YamlMember(Alias = "tls")]
    public List<IngressTLSV1> Tls { get; } = [];
}
