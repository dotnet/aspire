// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint reference for a resource with endpoints.
/// </summary>
public sealed class EndpointReference : IManifestExpressionProvider, IValueProvider, IValueWithReferences
{
    // A reference to the endpoint annotation if it exists.
    private EndpointAnnotation? _endpointAnnotation;
    private bool? _isAllocated;

    // TODO: Expose this
    internal EndpointAnnotation EndpointAnnotation => GetEndpointAnnotation() ?? throw new InvalidOperationException($"The endpoint `{EndpointName}` is not defined for the resource `{Resource.Name}`.");

    /// <summary>
    /// Gets the resource owner of the endpoint reference.
    /// </summary>
    public IResourceWithEndpoints Resource { get; }

    IEnumerable<object> IValueWithReferences.References => [Resource];

    /// <summary>
    /// Gets the name of the endpoint associated with the endpoint reference.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    /// Gets a value indicating whether the endpoint is allocated.
    /// </summary>
    public bool IsAllocated => _isAllocated ??= GetAllocatedEndpoint() is not null;

    string IManifestExpressionProvider.ValueExpression => GetExpression();

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(Url);

    /// <summary>
    /// Gets the specified property expression of the endpoint. Defaults to the URL if no property is specified.
    /// </summary>
    internal string GetExpression(EndpointProperty property = EndpointProperty.Url)
    {
        var prop = property switch
        {
            EndpointProperty.Url => "url",
            EndpointProperty.Host or EndpointProperty.IPV4Host => "host",
            EndpointProperty.Port => "port",
            EndpointProperty.Scheme => "scheme",
            _ => throw new InvalidOperationException($"The property '{property}' is not supported for the endpoint '{EndpointName}'.")
        };

        return $"{{{Resource.Name}.bindings.{EndpointName}.{prop}}}";
    }

    /// <summary>
    /// Gets the specified property expression of the endpoint. Defaults to the URL if no property is specified.
    /// </summary>
    /// <param name="property">The <see cref="EndpointProperty"/> enum value to use in the reference.</param>
    /// <returns>An <see cref="EndpointReferenceExpression"/> representing the specified <see cref="EndpointProperty"/>.</returns>
    public EndpointReferenceExpression Property(EndpointProperty property)
    {
#pragma warning disable CA2264 // Do not pass a non-nullable value to 'ArgumentNullException.ThrowIfNull'
        ArgumentNullException.ThrowIfNull(property);
#pragma warning restore CA2264 // Do not pass a non-nullable value to 'ArgumentNullException.ThrowIfNull'

        return new(this, property);
    }

    /// <summary>
    /// Gets the port for this endpoint.
    /// </summary>
    public int Port => AllocatedEndpoint.Port;

    /// <summary>
    /// Gets the host for this endpoint.
    /// </summary>
    public string Host => AllocatedEndpoint.Address ?? "localhost";

    /// <summary>
    /// Gets the container host for this endpoint.
    /// </summary>
    public string ContainerHost => AllocatedEndpoint.ContainerHostAddress ?? throw new InvalidOperationException($"The endpoint \"{EndpointName}\" has no associated container host name.");

    /// <summary>
    /// Gets the scheme for this endpoint.
    /// </summary>
    public string Scheme => AllocatedEndpoint.UriScheme;

    /// <summary>
    /// Gets the URL for this endpoint.
    /// </summary>
    public string Url => AllocatedEndpoint.UriString;

    private AllocatedEndpoint AllocatedEndpoint =>
        GetAllocatedEndpoint()
        ?? throw new InvalidOperationException($"The endpoint `{EndpointName}` is not allocated for the resource `{Resource.Name}`.");

    private EndpointAnnotation? GetEndpointAnnotation() =>
        _endpointAnnotation ??= Resource.Annotations.OfType<EndpointAnnotation>().SingleOrDefault(a => StringComparers.EndpointAnnotationName.Equals(a.Name, EndpointName));

    private AllocatedEndpoint? GetAllocatedEndpoint() => GetEndpointAnnotation()?.AllocatedEndpoint;

    /// <summary>
    /// Creates a new instance of <see cref="EndpointReference"/> with the specified endpoint name.
    /// </summary>
    /// <param name="owner">The resource with endpoints that owns the endpoint reference.</param>
    /// <param name="endpoint">The endpoint annotation.</param>
    public EndpointReference(IResourceWithEndpoints owner, EndpointAnnotation endpoint)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(endpoint);

        Resource = owner;
        EndpointName = endpoint.Name;
        _endpointAnnotation = endpoint;
    }

    /// <summary>
    /// Creates a new instance of <see cref="EndpointReference"/> with the specified endpoint name.
    /// </summary>
    /// <param name="owner">The resource with endpoints that owns the endpoint reference.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    public EndpointReference(IResourceWithEndpoints owner, string endpointName)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(endpointName);

        Resource = owner;
        EndpointName = endpointName;
    }
}

/// <summary>
/// Represents a property expression for an endpoint reference.
/// </summary>
/// <param name="endpointReference">The endpoint reference.</param>
/// <param name="property">The property of the endpoint.</param>
public class EndpointReferenceExpression(EndpointReference endpointReference, EndpointProperty property) : IManifestExpressionProvider, IValueProvider, IValueWithReferences
{
    /// <summary>
    /// Gets the <see cref="EndpointReference"/>.
    /// </summary>
    public EndpointReference Endpoint { get; } = endpointReference ?? throw new ArgumentNullException(nameof(endpointReference));

    /// <summary>
    /// Gets the <see cref="EndpointProperty"/> for the property expression.
    /// </summary>
    public EndpointProperty Property { get; } = property;

    /// <summary>
    /// Gets the expression of the property of the endpoint.
    /// </summary>
    public string ValueExpression =>
        Endpoint.GetExpression(Property);

    /// <summary>
    /// Gets the value of the property of the endpoint.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="String"/> containing the selected <see cref="EndpointProperty"/> value.</returns>
    /// <exception cref="InvalidOperationException">Throws when the selected <see cref="EndpointProperty"/> enumeration is not known.</exception>
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken) => Property switch
    {
        EndpointProperty.Url => new(Endpoint.Url),
        EndpointProperty.Host => new(Endpoint.Host),
        EndpointProperty.IPV4Host => new("127.0.0.1"),
        EndpointProperty.Port => new(Endpoint.Port.ToString(CultureInfo.InvariantCulture)),
        EndpointProperty.Scheme => new(Endpoint.Scheme),
        _ => throw new InvalidOperationException($"The property '{Property}' is not supported for the endpoint '{Endpoint.EndpointName}'.")
    };

    IEnumerable<object> IValueWithReferences.References => [Endpoint];
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
    /// The IPv4 address of the endpoint.
    /// </summary>
    IPV4Host,
    /// <summary>
    /// The port of the endpoint.
    /// </summary>
    Port,
    /// <summary>
    /// The scheme of the endpoint.
    /// </summary>
    Scheme
}
