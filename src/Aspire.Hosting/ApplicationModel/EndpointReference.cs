// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint reference for a resource with endpoints.
/// </summary>
public sealed class EndpointReference : IManifestExpressionProvider, IValueProvider
{
    // A reference to the allocated endpoint annotation if it exists.
    private AllocatedEndpointAnnotation? _allocatedEndpointAnnotation;
    private bool? _isAllocated;

    /// <summary>
    /// Gets the owner of the endpoint reference.
    /// </summary>
    public IResourceWithEndpoints Owner { get; }

    /// <summary>
    /// Gets the name of the endpoint associated with the endpoint reference.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    /// Gets a value indicating whether the endpoint is allocated.
    /// </summary>
    public bool IsAllocated => _isAllocated ??= _allocatedEndpointAnnotation is not null || GetAllocatedEndpoint() is not null;

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
            _ => throw new InvalidOperationException($"The property '{property}' is not supported for the endpoint '{EndpointName}'.")
        };

        return $"{{{Owner.Name}.bindings.{EndpointName}.{prop}}}";
    }

    /// <summary>
    /// Gets the port for this endpoint.
    /// </summary>
    public int Port => AllocatedEndpointAnnotation.Port;

    /// <summary>
    /// Gets the host for this endpoint.
    /// </summary>
    public string Host => AllocatedEndpointAnnotation.Address ?? "localhost";

    /// <summary>
    /// Gets the scheme for this endpoint.
    /// </summary>
    public string Scheme => AllocatedEndpointAnnotation.UriScheme;

    /// <summary>
    /// Gets the URL for this endpoint.
    /// </summary>
    public string Url => AllocatedEndpointAnnotation.UriString;

    private AllocatedEndpointAnnotation AllocatedEndpointAnnotation =>
        _allocatedEndpointAnnotation ??= GetAllocatedEndpoint()
        ?? throw new InvalidOperationException($"The endpoint `{EndpointName}` is not allocated for the resource `{Owner.Name}`.");

    private AllocatedEndpointAnnotation? GetAllocatedEndpoint() =>
        Owner.Annotations.OfType<AllocatedEndpointAnnotation>()
             .SingleOrDefault(a => StringComparers.EndpointAnnotationName.Equals(a.Name, EndpointName));

    /// <summary>
    /// Creates a new instance of <see cref="EndpointReference"/> with the specified endpoint name.
    /// </summary>
    /// <param name="owner">The resource with endpoints that owns the endpoint reference.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    public EndpointReference(IResourceWithEndpoints owner, string endpointName)
    {
        Owner = owner;
        EndpointName = endpointName;
    }

    /// <summary>
    /// Creates a new instance of <see cref="EndpointReference"/> with the specified allocated endpoint annotation.
    /// </summary>
    /// <param name="owner">The resource with endpoints that owns the endpoint reference.</param>
    /// <param name="allocatedEndpointAnnotation"> The allocated endpoint annotation.</param>
    public EndpointReference(IResourceWithEndpoints owner, AllocatedEndpointAnnotation allocatedEndpointAnnotation)
    {
        Owner = owner;
        EndpointName = allocatedEndpointAnnotation.Name;
        _allocatedEndpointAnnotation = allocatedEndpointAnnotation;
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
