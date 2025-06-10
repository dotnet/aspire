// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an Ingress resource in Kubernetes (networking.k8s.io/v1).
/// </summary>
/// <remarks>
/// The Ingress class is a sealed implementation of a Kubernetes resource used to expose HTTP and HTTPS routes
/// to services within a cluster. It provides a mechanism to define rules for routing external traffic to specific
/// backends, as well as support for setting TLS configurations, default backends, and ingress class names.
/// Inherits from the BaseKubernetesResource with "networking.k8s.io/v1" as the API version and "Ingress" as its kind.
/// </remarks>
[YamlSerializable]
public sealed class Ingress() : BaseKubernetesResource("networking.k8s.io/v1", "Ingress")
{
    /// <summary>
    /// Gets or sets the specification of the Ingress resource.
    /// </summary>
    /// <remarks>
    /// The Spec property represents the configuration for the Ingress resource in Kubernetes. It contains
    /// details about backends, rules, TLS settings, and ingress class names, allowing control over how HTTP
    /// and HTTPS traffic is routed within the cluster. The value of this property is defined by the
    /// IngressSpecV1 class.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public IngressSpecV1 Spec { get; set; } = new();
}
