// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a Kubernetes Service resource in the v1 API version.
/// </summary>
/// <remarks>
/// The Service class is used to define and configure networking Services within a Kubernetes cluster.
/// It inherits common Kubernetes resource attributes from the BaseKubernetesResource class and provides
/// additional specific configurations through its Spec property, which describes the desired behavior of the Service.
/// </remarks>
[YamlSerializable]
public sealed class Service() : BaseKubernetesResource("v1", "Service")
{
    /// <summary>
    /// Represents the specification of the Kubernetes Service resource.
    /// </summary>
    /// <remarks>
    /// This property contains the configuration details of the Service,
    /// such as ClusterIP, LoadBalancerIP, type, selector, ports, and other service-specific settings.
    /// The values are represented by the <see cref="ServiceSpecV1"/> class.
    /// </remarks>
    [YamlMember(Alias = "spec")]
    public ServiceSpecV1 Spec { get; set; } = new();
}
