// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the HTTP ingress rule configuration for Kubernetes resources.
/// </summary>
/// <remarks>
/// This class defines the HTTP routing rules within a Kubernetes ingress resource. Each rule contains
/// a collection of paths that specify how HTTP traffic is routed to backend services. It is used to
/// match requests based on specified HTTP path patterns.
/// </remarks>
[YamlSerializable]
public sealed class HttpIngressRuleValueV1
{
    /// <summary>
    /// Gets the list of HTTP ingress paths for the current ingress rule.
    /// </summary>
    /// <remarks>
    /// Each path is represented by an instance of <see cref="HttpIngressPathV1"/>.
    /// These paths define the routing configuration for HTTP requests, including
    /// criteria for matching and the destination backend service or resource.
    /// </remarks>
    [YamlMember(Alias = "paths")]
    public List<HttpIngressPathV1> Paths { get; } = [];
}
