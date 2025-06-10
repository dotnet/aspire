// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a path in an HTTP ingress rule for Kubernetes ingress resources.
/// </summary>
/// <remarks>
/// An HTTP ingress path defines the configuration for matching incoming requests to a specific backend service
/// based on the provided path and path type. It includes details about the service or resource to route
/// the traffic to and the criteria used to determine when the path matches the incoming request.
/// </remarks>
[YamlSerializable]
public sealed class HttpIngressPathV1
{
    /// <summary>
    /// Represents the backend configuration for a specific path of an HTTP ingress.
    /// This property defines the backend service or resource to which the traffic
    /// will be forwarded. The `Backend` object can reference either a service
    /// (specified with `IngressServiceBackendV1`) or a resource
    /// (specified with `TypedLocalObjectReferenceV1`) to handle the incoming requests.
    /// </summary>
    [YamlMember(Alias = "backend")]
    public IngressBackendV1 Backend { get; set; } = new();

    /// <summary>
    /// Gets or sets the type of path used in the ingress rule.
    /// Indicates the interpretation of the path such as Exact, Prefix, or ImplementationSpecific.
    /// </summary>
    [YamlMember(Alias = "pathType")]
    public string PathType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path that the Ingress matches against incoming requests.
    /// The value should specify a valid URI path which determines where the routing rules apply.
    /// </summary>
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = null!;
}
