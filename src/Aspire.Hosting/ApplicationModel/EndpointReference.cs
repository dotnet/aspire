// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint reference for a resource with endpoints.
/// </summary>
/// <param name="owner">The resource with endpoints that owns the endpoint reference.</param>
/// <param name="endpointName">The name of the endpoint.</param>
public sealed class EndpointReference(IResourceWithEndpoints owner, string endpointName) : IManifestExpressionProvider, IValueProvider
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
    /// Gets a value indicating whether the endpoint is allocated.
    /// </summary>
    public bool IsAllocated => Owner.Annotations.OfType<AllocatedEndpointAnnotation>()
        .Any(a => StringComparers.EndpointAnnotationName.Equals(a.Name, EndpointName));

    string IManifestExpressionProvider.ValueExpression => GetExpression();

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(Url);

    /// <summary>
    /// Gets the specified property expression of the endpoint. Defaults to the URL if no property is specified.
    /// </summary>
    public string GetExpression(EndpointProperty property = EndpointProperty.Url)
    {
        var prop = property switch
        {
            EndpointProperty.Url => "url",
            EndpointProperty.Host => "host",
            EndpointProperty.Port => "port",
            EndpointProperty.Scheme => "scheme",
            _ => throw new InvalidOperationException($"The property `{property}` is not supported for the endpoint `{EndpointName}`.")
        };

        return $"{{{Owner.Name}.bindings.{EndpointName}.{prop}}}";
    }

    /// <summary>
    /// Gets the port for this endpoint.
    /// </summary>
    public int Port => GetAllocatedEndpoint().Port;

    /// <summary>
    /// Gets the host for this endpoint.
    /// </summary>
    public string Host => GetAllocatedEndpoint().Address ?? "localhost";

    /// <summary>
    /// Gets the scheme for this endpoint.
    /// </summary>
    public string Scheme => GetAllocatedEndpoint().UriScheme;

    /// <summary>
    /// Gets the URL for this endpoint.
    /// </summary>
    public string Url => GetAllocatedEndpoint().UriString;

    private AllocatedEndpointAnnotation GetAllocatedEndpoint()
    {
        var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>()
            .SingleOrDefault(a => StringComparers.EndpointAnnotationName.Equals(a.Name, EndpointName)) ??
            throw new InvalidOperationException($"The endpoint `{EndpointName}` is not allocated for the resource `{Owner.Name}`.");

        return allocatedEndpoint;
    }
}

/// <summary>
/// Represents the properties of an endpoint that can be referenced.
/// </summary>
public enum EndpointProperty
{
    /// <summary>
    /// The entire URL of the endpoint.
    /// </summary>
    Url,
    /// <summary>
    /// The host of the endpoint.
    /// </summary>
    Host,
    /// <summary>
    /// The port of the endpoint.
    /// </summary>
    Port,
    /// <summary>
    /// The scheme of the endpoint.
    /// </summary>
    Scheme
}
