// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint reference for a resource with endpoints.
/// </summary>
/// <param name="owner">The resource with endpoints that owns the endpoint reference.</param>
/// <param name="endpointName">The name of the endpoint.</param>
public sealed class EndpointReference(IResourceWithEndpoints owner, string endpointName)
{
    /// <summary>
    /// Gets the owner of the endpoint reference.
    /// </summary>
    public IResourceWithEndpoints Owner { get; } = owner;

    /// <summary>
    /// Gets the name of the endpoint associated with the endpoint reference.
    /// </summary>
    public string EndpointName { get; } = endpointName;

    /// <summary>
    /// Gets the expression used in the manifest to reference the value of the endpoint.
    /// </summary>
    public string ValueExpression => $"{{{Owner.Name}.bindings.{EndpointName}.url}}";

    /// <summary>
    /// Gets the URI string for the endpoint reference.
    /// </summary>
    public string Value
    {
        get
        {
            var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault(a => a.Name == EndpointName);

            return allocatedEndpoint?.UriString ??
                throw new InvalidOperationException($"The endpoint `{EndpointName}` is not allocated for the resource `{Owner.Name}`.");
        }
    }

    /// <summary>
    /// Gets the URI string for the endpoint reference.
    /// </summary>
    [Obsolete("Use Value instead.")]
    public string UriString
    {
        get
        {
            var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault(a => a.Name == EndpointName);
            return allocatedEndpoint?.UriString ?? $"{{{Owner.Name}.bindings.{EndpointName}.url}}";
        }
    }
}
