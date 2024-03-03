// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

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

    string IManifestExpressionProvider.ValueExpression => GetValueExpression();

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => new(GetValue());

    /// <summary>
    /// Gets the specified property expression of the endpoint. Defaults to the URL if no property is specified.
    /// </summary>
    public string GetValueExpression(EndpointProperty property = EndpointProperty.Url)
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
    /// Gets the specified property value of the endpoint. Defaults to the URL if no property is specified.
    /// </summary>
    public string GetValue(EndpointProperty property = EndpointProperty.Url)
    {
        var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault(a => a.Name == EndpointName);

        if (allocatedEndpoint is null)
        {
            throw new InvalidOperationException($"The endpoint `{EndpointName}` is not allocated for the resource `{Owner.Name}`.");
        }

        return property switch
        {
            EndpointProperty.Url => allocatedEndpoint.UriString,
            EndpointProperty.Host => allocatedEndpoint.Address ?? "localhost",
            EndpointProperty.Port => allocatedEndpoint.Port.ToString(CultureInfo.InvariantCulture),
            EndpointProperty.Scheme => allocatedEndpoint.UriScheme,
            _ => throw new InvalidOperationException($"The property `{property}` is not supported for the endpoint `{EndpointName}`.")
        };
    }

    /// <summary>
    /// Gets the URI string for the endpoint reference.
    /// </summary>
    [Obsolete("Use GetValue instead.")]
    public string UriString
    {
        get
        {
            var allocatedEndpoint = Owner.Annotations.OfType<AllocatedEndpointAnnotation>().SingleOrDefault(a => a.Name == EndpointName);
            return allocatedEndpoint?.UriString ?? $"{{{Owner.Name}.bindings.{EndpointName}.url}}";
        }
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
