// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an ingress rule for Kubernetes resources.
/// </summary>
/// <remarks>
/// This class defines a rule within a Kubernetes ingress resource that specifies the routing configuration
/// for incoming network traffic. It includes an optional host and detailed HTTP rule settings.
/// </remarks>
[YamlSerializable]
public sealed class IngressRuleV1
{
    /// <summary>
    /// Represents the HTTP ingress rule value associated with this ingress rule.
    /// </summary>
    /// <remarks>
    /// This property defines the HTTP routing rules, including paths and backend services, for managing HTTP
    /// traffic in a Kubernetes ingress resource. It specifies how requests are matched and routed based
    /// on path patterns provided in the configuration.
    /// </remarks>
    [YamlMember(Alias = "http")]
    public HttpIngressRuleValueV1 Http { get; set; } = new();

    /// <summary>
    /// Gets or sets the host for the ingress rule in Kubernetes.
    /// </summary>
    /// <remarks>
    /// The <c>Host</c> property is used to specify the fully qualified domain name (FQDN)
    /// that is matched for the ingress rule. It helps in routing incoming network traffic
    /// based on the host name specified in HTTP requests. If left empty, the ingress rule
    /// applies to all incoming traffic irrespective of the host name.
    /// </remarks>
    [YamlMember(Alias = "host")]
    public string Host { get; set; } = null!;
}
